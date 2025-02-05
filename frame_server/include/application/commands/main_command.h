#pragma once

#include "application/logger.h"
#include "viana/application/commands/command.h"
#include "viana/application/http.h"
#include "viana/application/couchdb.h"
#include "viana/application/sa_installer.h"
#include "viana/application/common_include.h"
#include "viana/application/concurrent_queue.h"
#include "viana/application/mqtt.h"
#include "viana/domain/cloud_auth.h"
#include "viana/domain/args.h"

#include "application/is_alive.h"
#include "application/resilience_protocol.h"
#include "application/sensor_command_consumer.h"
#include "application/service_command_consumer.h"
#include "application/camera.h"
#include "domain/main_args.h"

static int myErrorHandler(int, const char*, const char*, const char*, int, void*) {
	return 0;
}

struct MainCommand : Command<MainArgs> {
	CloudAuth cloudAuth;

	inline MainCommand(std::shared_ptr<Logger> parentLogger = nullptr) : Command<MainArgs>("MAIN", parentLogger) {
		cv::utils::logging::setLogLevel(cv::utils::logging::LogLevel::LOG_LEVEL_SILENT);
		cv::redirectError(myErrorHandler);

		log->attach(IsAlive::log);
		log->attach(ResilienceProtocol::log);
		log->attach(Camera::log);
		log->attach(SensorCommandConsumer::log);
		log->attach(ServiceCommandConsumer::log);
	}

	int runInternal() override;
};
