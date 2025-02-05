#pragma once

#include "application/common_include.h"
#include "domain/json_result.h"

struct Common {
    const static inline std::string UID_CHARS =
            "0123456789abcdefghijklmnopqrstuvwxyz";

    const static inline std::string BASE64_CHARS =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
            "abcdefghijklmnopqrstuvwxyz"
            "0123456789+/";

    const static inline json DEFAULT_LOGGING = {
            {"level", "info"}
    };

    template<typename... Args>
    static inline std::string fmt(std::string fmt, Args&&... args) {
        return fmt::vformat(fmt, fmt::v10::make_format_args(args...));
    }

    static uint64_t getEpochTimeMs();

    static std::string epochTimeMsToReadable(uint64_t epoch_time_ms);

    static std::string utcDateReadable();

    static std::string getHostname();

    static bool is_base64(unsigned char c);

    static std::string toBase64(std::string data);

    static std::string fromBase64(std::string base64);

    static std::string readFile(std::filesystem::path filename);

    static void writeFile(std::filesystem::path filename, std::string content);

    static void copyFile(std::filesystem::path source, std::filesystem::path dest, bool skipError = false);

    static void copyAllFiles(std::filesystem::path sourceDir, std::filesystem::path destDir, std::vector<std::string> names = {}, bool skipError = false);

    static std::string removeSpacesAndSpecialCharacter(std::string s);

    static std::string strToLower(std::string s);

    static std::string strToUpper(std::string s);

    static void ltrim(std::string& s, std::optional<unsigned char> toTrim = std::optional<unsigned char>());

    static void rtrim(std::string& s, std::optional<unsigned char> toTrim = std::optional<unsigned char>());

    static void trim(std::string& s, std::optional<unsigned char> toTrim = std::optional<unsigned char>());

    static std::string ltrimCopy(std::string s, std::optional<unsigned char> toTrim = std::optional<unsigned char>());

    static std::string rtrimCopy(std::string s, std::optional<unsigned char> toTrim = std::optional<unsigned char>());

    static std::string trimCopy(std::string s, std::optional<unsigned char> toTrim = std::optional<unsigned char>());

    static std::vector<std::string> splitStr(std::string s, std::vector<std::string> delimiters, bool removeEmpty = false);

    static std::string joinStr(const std::vector<std::string>& lst, std::string delim, bool removeEmpty = false);

    static std::string removeStr(std::string s, std::vector<std::string> toRemove, bool caseSensitive = true);

    static std::string replaceStr(std::string s, const std::string& from, const std::string& to, bool caseSensitive = true);

    static bool startsWith(std::string s, std::string start);

    static bool endsWith(std::string s, std::string end);

    static bool isElevated();

    static std::string randomUuid();

#if defined(WINDOWS)
    static std::string getStrFromRegistry(std::string keyPath);

    static void setStrFromRegistry(std::string keyPath, std::string value);

    static std::string bstrToString(BSTR bstr);
#endif
};
