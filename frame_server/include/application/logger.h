#pragma once

#include "application/common_include.h"
#include "application/common.h"

struct Logger {
    static const inline std::string DEBUG_L = "DEBUG";
    static const inline std::string INFO_L = "INFO";
    static const inline std::string WARNING_L = "WARNING";
    static const inline std::string ERROR_L = "ERROR";
    static const inline std::string CRITICAL_L = "CRITICAL";
    static const inline std::vector<std::string> LoggingLevels{ DEBUG_L, INFO_L, WARNING_L, ERROR_L, CRITICAL_L };

    static const inline std::string LOG_PATTERN = "[%Y-%m-%d %H:%M:%S.%e] %-15n %^%-8l%$ -- %v";

    static void _internalSetLoggingLevel(std::shared_ptr<spdlog::logger> logger, std::string _loggingLevel) {
        if (Common::strToUpper(_loggingLevel) == DEBUG_L) {
            logger->set_level(spdlog::level::debug);
        }
        else if (Common::strToUpper(_loggingLevel) == INFO_L) {
            logger->set_level(spdlog::level::info);
        }
        else if (Common::strToUpper(_loggingLevel) == WARNING_L) {
            logger->set_level(spdlog::level::warn);
        }
        else if (Common::strToUpper(_loggingLevel) == ERROR_L) {
            logger->set_level(spdlog::level::err);
        }
        else if (Common::strToUpper(_loggingLevel) == CRITICAL_L) {
            logger->set_level(spdlog::level::critical);
        }
        else {
            throw std::runtime_error("Logging level \"" + _loggingLevel + "\" does not exists.");
        }
    }

    static std::shared_ptr<Logger> generate(std::string name, std::string logFile, std::shared_ptr<Logger> parentLogger = nullptr) {
        std::shared_ptr<Logger> logger;
        if (logFile != "") {
            std::filesystem::path logFilePath(logFile);
            std::string logFilename = (logFilePath.parent_path() / logFilePath.stem()).string();
            std::string logExtension = logFilePath.extension().string();
            std::string debugLogFile = logFilename + ".debug" + logExtension;
            std::string infoLogFile = logFilename + ".info" + logExtension;
            std::string warningLogFile = logFilename + ".warning" + logExtension;
            std::string errorLogFile = logFilename + ".error" + logExtension;
            std::string criticalLogFile = logFilename + ".critical" + logExtension;
            auto log = std::make_shared<spdlog::logger>(spdlog::logger(name, { std::make_shared<spdlog::sinks::stdout_color_sink_mt>() }));
            auto debugLog = std::make_shared<spdlog::logger>(spdlog::logger(name, { std::make_shared<spdlog::sinks::daily_file_sink_mt>(debugLogFile, 23, 59) }));
            auto infoLog = std::make_shared<spdlog::logger>(spdlog::logger(name, { std::make_shared<spdlog::sinks::daily_file_sink_mt>(infoLogFile, 23, 59) }));
            auto warningLog = std::make_shared<spdlog::logger>(spdlog::logger(name, { std::make_shared<spdlog::sinks::daily_file_sink_mt>(warningLogFile, 23, 59) }));
            auto errorLog = std::make_shared<spdlog::logger>(spdlog::logger(name, { std::make_shared<spdlog::sinks::daily_file_sink_mt>(errorLogFile, 23, 59) }));
            auto criticalLog = std::make_shared<spdlog::logger>(spdlog::logger(name, { std::make_shared<spdlog::sinks::daily_file_sink_mt>(criticalLogFile, 23, 59) }));
            log->set_pattern(LOG_PATTERN);
            debugLog->set_pattern(LOG_PATTERN);
            infoLog->set_pattern(LOG_PATTERN);
            warningLog->set_pattern(LOG_PATTERN);
            errorLog->set_pattern(LOG_PATTERN);
            criticalLog->set_pattern(LOG_PATTERN);
            _internalSetLoggingLevel(debugLog, DEBUG_L);
            _internalSetLoggingLevel(infoLog, INFO_L);
            _internalSetLoggingLevel(warningLog, WARNING_L);
            _internalSetLoggingLevel(errorLog, ERROR_L);
            _internalSetLoggingLevel(criticalLog, CRITICAL_L);
            logger = std::make_shared<Logger>(Logger{ true, log, debugLog, infoLog, warningLog, errorLog, criticalLog });
        }
        else {
            auto log = std::make_shared<spdlog::logger>(spdlog::logger(name, { std::make_shared<spdlog::sinks::stdout_color_sink_mt>() }));
            log->set_pattern(LOG_PATTERN);
            logger = std::make_shared<Logger>(Logger{ false, log });
        }
        if (parentLogger != nullptr) {
            parentLogger->attach(logger);
        }
        return logger;
    }

    static std::shared_ptr<Logger> generate(std::string name, std::shared_ptr<Logger> parentLogger = nullptr) {
        return generate(name, "", parentLogger);
    }

    static int getRank(std::string logingLevel) {
        std::string logingLevelUpper = Common::trimCopy(Common::strToUpper(logingLevel));
        for (int i = 0; i < LoggingLevels.size(); i++) {
            if (LoggingLevels[i] == logingLevelUpper) {
                return i;
            }
        }
        throw std::runtime_error("Logging level \"" + logingLevel + "\" does not exists.");
        return -1;
    }

    static bool isSilent(std::string argsLoggingLevel, std::string logingLevel) {
        return getRank(argsLoggingLevel) > getRank(logingLevel);
    }

    bool hasLoggerFile;

    std::shared_ptr<spdlog::logger> internalLogger;

    std::shared_ptr<spdlog::logger> internalDebugLogger;

    std::shared_ptr<spdlog::logger> internalInfoLogger;

    std::shared_ptr<spdlog::logger> internalWarningLogger;

    std::shared_ptr<spdlog::logger> internalErrorLogger;

    std::shared_ptr<spdlog::logger> internalCriticalLogger;

    std::string loggingLevel = INFO_L;

    bool asJson = false;

    std::vector<std::shared_ptr<Logger>> attachedLoggers{};

    void updateAttachedLoggers() {
        for (auto& attachedLogger : attachedLoggers) {
            attachedLogger->setLoggingLevel(loggingLevel);
            attachedLogger->setAsJson(asJson);
        }
    }

    void setLoggingLevel(std::string _loggingLevel) {
        loggingLevel = _loggingLevel;
        _internalSetLoggingLevel(internalLogger, _loggingLevel);
        updateAttachedLoggers();
    }

    void setAsJson(bool _asJson) {
        asJson = _asJson;
        updateAttachedLoggers();
    }

    void attach(std::shared_ptr<Logger> logger) {
        attachedLoggers.emplace_back(logger);
        updateAttachedLoggers();
    }

    void debug(std::string msg) const {
        if (!isSilent(loggingLevel, "debug")) {
            internalLogger->debug(msg);
            internalLogger->flush();
        }
        if (hasLoggerFile) {
            internalDebugLogger->debug(msg);
            internalDebugLogger->flush();
        }
    }

    void info(std::string msg) const {
        if (!isSilent(loggingLevel, "info")) {
            internalLogger->info(msg);
            internalLogger->flush();
        }
        if (hasLoggerFile) {
            internalDebugLogger->info(msg);
            internalInfoLogger->info(msg);
            internalDebugLogger->flush();
            internalInfoLogger->flush();
        }
    }

    void warning(std::string msg) const {
        if (!isSilent(loggingLevel, "warning")) {
            internalLogger->warn(msg);
            internalLogger->flush();
        }
        if (hasLoggerFile) {
            internalDebugLogger->warn(msg);
            internalInfoLogger->warn(msg);
            internalWarningLogger->warn(msg);
            internalDebugLogger->flush();
            internalInfoLogger->flush();
            internalWarningLogger->flush();
        }
    }

    void error(std::string msg) const {
        if (!isSilent(loggingLevel, "error")) {
            internalLogger->error(msg);
            internalLogger->flush();
        }
        if (hasLoggerFile) {
            internalDebugLogger->error(msg);
            internalInfoLogger->error(msg);
            internalErrorLogger->error(msg);
            internalDebugLogger->flush();
            internalInfoLogger->flush();
            internalErrorLogger->flush();
        }
    }

    void critical(std::string msg) const {
        if (!isSilent(loggingLevel, "critical")) {
            internalLogger->error(msg);
            internalLogger->flush();
        }
        if (hasLoggerFile) {
            internalDebugLogger->error(msg);
            internalInfoLogger->error(msg);
            internalErrorLogger->error(msg);
            internalCriticalLogger->critical(msg);
            internalDebugLogger->flush();
            internalInfoLogger->flush();
            internalErrorLogger->flush();
            internalCriticalLogger->flush();
        }
    }

    void debug(std::string code, std::string msg, json::object_t jsonResult = { }) const {
        if (asJson) {
            std::cout << JsonResult{ internalLogger->name(), DEBUG_L, code, msg, jsonResult }.toJson() << std::endl;
        }
        else {
            for (auto& s : Common::splitStr(msg, { "\n" })) {
                debug(s);
            }
        }
    }

    void info(std::string code, std::string msg, json::object_t jsonResult = { }) const {
        if (asJson) {
            std::cout << JsonResult{ internalLogger->name(), INFO_L, code, msg, jsonResult }.toJson() << std::endl;
        }
        else {
            for (auto& s : Common::splitStr(msg, { "\n" })) {
                info(s);
            }
        }
    }

    void warning(std::string code, std::string msg, json::object_t jsonResult = { }) const {
        if (asJson) {
            std::cout << JsonResult{ internalLogger->name(), WARNING_L, code, msg, jsonResult }.toJson() << std::endl;
        }
        else {
            for (auto& s : Common::splitStr(msg, { "\n" })) {
                warning(s);
            }
        }
    }

    void error(std::string code, std::string msg, json::object_t jsonResult = { }) const {
        if (asJson) {
            std::cout << JsonResult{ internalLogger->name(), ERROR_L, code, msg, jsonResult }.toJson() << std::endl;
        }
        else {
            for (auto& s : Common::splitStr(msg, { "\n" })) {
                error(s);
            }
        }
    }

    void critical(std::string code, std::string msg, json::object_t jsonResult = { }) const {
        if (asJson) {
            std::cout << JsonResult{ internalLogger->name(), CRITICAL_L, code, msg, jsonResult }.toJson() << std::endl;
        }
        else {
            for (auto& s : Common::splitStr(msg, { "\n" })) {
                critical(s);
            }
        }
    }
};
