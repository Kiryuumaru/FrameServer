#include "application/commands/main_command.h"

int MainCommand::runInternal() {

	cloudAuth = Cloud::getCloudAuth();

	if (!cloudAuth->installed && args.noWaitInit.value) {
		Cloud::initDevice(cloudAuth);
		cloudAuth->installed = true;
		Cloud::setCloudAuth(cloudAuth, false);
	}

	while (true) {
		cloudAuth = Cloud::getCloudAuth();
		if (!cloudAuth->installed) {
			log->warning("INSTALLATION_STATUS_WAIT", "Waiting for installation indicator");
			std::this_thread::sleep_for(std::chrono::seconds(2));
		}
		else {
			break;
		}
	}

	std::string tag = cloudAuth->appTag;

	log->info("ENV_RUNTIME", Common::fmt("Running in {}{} environment", Configs::getEnv(tag), " (" + tag + ")"));

	std::string caFile = (Device::getCertsDir() / "vianad-ca.pem").string();

	while (!std::filesystem::exists(caFile)) {
		try {
			Downloader::download({ cloudAuth->fmtCertUrl("ca." + cloudAuth->appTag + ".pem")}, caFile, {{"Authorization", cloudAuth->getAuthBasicHeader()}}, true);
			break;
		}
		catch (std::exception& ex) {
			log->error("INSTALL_VIANAD_SERVICE_ERROR", "Initializing vianad certs error: " + std::string(ex.what()) + ". Retrying...");
			std::this_thread::sleep_for(std::chrono::milliseconds(1000));
		}
	}

	CouchDB::waitForAlive(Configs::getCouchdbApiAddress());
	CouchDB edieDB(Constants::COUCHDB_EDIE_ENDPOINT, Configs::getCouchdbApiAddress());
	CouchDB deviceDB(Constants::COUCHDB_DEVICE_ENDPOINT, Configs::getCouchdbApiAddress());
	CouchDB sensorsDB(Constants::COUCHDB_SENSORS_ENDPOINT, Configs::getCouchdbApiAddress());
	CouchDB sensorsBAKDB(Constants::COUCHDB_SENSORS_BAK_ENDPOINT, Configs::getCouchdbApiAddress());

	edieDB.create();
	deviceDB.create();
	sensorsDB.create();
	sensorsBAKDB.create();

	ThreadCompat resilienceProtocol = ResilienceProtocol::startAsync(cloudAuth);
	ThreadCompat heartbeat = IsAlive::startAsync(cloudAuth);

	std::this_thread::sleep_for(std::chrono::seconds(5));

	ConcurrentQueue<json> sensorCommandQueue{};
	ConcurrentQueue<json> serviceCommandQueue{};
	ThreadCompat commandMqtt = Mqtt::connectAsync(cloudAuth, Common::fmt("/device/edge/{}/command", cloudAuth->deviceConfig["serial_identifier"].get<std::string>()),
		[&](std::string payload) {
			json jsonPayload = json::parse(payload);
			std::string action = Common::strToLower(jsonPayload["action"]);
			if (action.find('_') != std::string::npos) {
				std::vector<std::string> actionSplit = Common::splitStr(action, { "_" });
				std::string actionCategory = actionSplit[0];
				if (actionCategory == "sensor") {
					sensorCommandQueue->push(jsonPayload);
					return;
				}
				else if (actionCategory == "service") {
					serviceCommandQueue->push(jsonPayload);
					return;
				}
			}

			log->error("COMMAND_QUEUE_ERROR", "Unknown command queue: " + payload);
		}, {});
	ThreadCompat sensorCommandConsumer = SensorCommandConsumer::startAsync(cloudAuth);
	//ThreadCompat serviceCommandConsumer = ServiceCommandConsumer::startAsync(cloudAuth, serviceCommandQueue);

	ThreadCompat({ commandMqtt, sensorCommandConsumer, heartbeat, resilienceProtocol })->wait();
	//ThreadCompat({ commandMqtt, sensorCommandConsumer, serviceCommandConsumer, heartbeat, resilienceProtocol })->wait();

	return 0;
}
