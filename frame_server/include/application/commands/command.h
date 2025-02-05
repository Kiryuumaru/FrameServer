#pragma once

#include "application/common_include.h"
#include "application/common.h"
#include "application/cancellation_token.h"
#include "application/thread_compat.h"
#include "application/scheduler.h"
#include "domain/args.h"

#pragma execution_character_set("utf-8")

template <typename TArgs>
struct Command {

    inline static std::string MOTD =
        "\n"
        "\n"
        "   ██╗   ██╗██╗ █████╗ ███╗   ██╗ █████╗  \n"
        "   ██║   ██║██║██╔══██╗████╗  ██║██╔══██╗ \n"
        "   ██║   ██║██║███████║██╔██╗ ██║███████║ \n"
        "   ╚██╗ ██╔╝██║██╔══██║██║╚██╗██║██╔══██║ \n"
        "    ╚████╔╝ ██║██║  ██║██║ ╚████║██║  ██║ \n"
        "     ╚═══╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝ \n"
        "                                by meldCX \n"
        "\n"
        "   " + std::string(PROJECT_DISPLAY_NAME) + "\n"
        "   v" + std::string(PROJECT_FULL_VERSION) + "\n"
        "\n";

	TArgs args;

	std::shared_ptr<Logger> log;

    inline Command(std::string logName, std::shared_ptr<Logger> parentLogger = nullptr) : log(Logger::generate(logName, parentLogger)) { }

    inline Command(std::string logName, std::string logFile, std::shared_ptr<Logger> parentLogger = nullptr) : log(Logger::generate(logName, logFile, parentLogger)) { }

    virtual int runInternal() = 0;

    int run(TArgs _args) {
        args = _args;
        Args* baseArgs = dynamic_cast<Args*>(&_args);
        if (!baseArgs->asJson.value) {
            std::cout << MOTD;
        }
        log->setLoggingLevel(baseArgs->logLevel.value);
        log->setAsJson(baseArgs->asJson.value);
        log->attach(Cli::log);
        log->attach(Http::log);
        log->attach(Cloud::log);
        log->attach(Downloader::log);
        log->attach(SAInstaller::log);
        log->attach(Mqtt::log);
        log->attach(Reset::log);
        return runInternal();
    }

    int run(Args mainArgs, TArgs _args) {
        Args* baseArgs = dynamic_cast<Args*>(&_args);
        baseArgs->patch(mainArgs);
        return run(_args);
    }

	ThreadCompat checkConnectivityAsync(CancellationToken ct) {
        std::this_thread::sleep_for(std::chrono::seconds(10));
        bool isConnected = true;
        return Scheduler::createSeconds(10, [&, isConnected = std::move(isConnected), ct]() mutable {
            try {
                Http::get("https://ip.viana.ai");
                if (!isConnected) {
                    isConnected = true;
                    log->info("NET_ONLINE", "Internet is back online");
                }
            }
            catch (std::exception& ex) {
                log->error("NET_OFFLINE", "No internet connection: " + std::string(ex.what()));
                if (isConnected) {
                    isConnected = false;
                }
            }
            }, ct);
    }
};
