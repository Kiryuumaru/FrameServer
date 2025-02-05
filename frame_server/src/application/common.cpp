#include "application/common.h"

#include <openssl/bio.h>
#include <openssl/buffer.h>

#include <boost/uuid/uuid.hpp>
#include <boost/uuid/uuid_generators.hpp>
#include <boost/uuid/uuid_io.hpp>
#include <boost/lexical_cast.hpp>

uint64_t Common::getEpochTimeMs() {
    return std::chrono::duration_cast<std::chrono::milliseconds>(
        std::chrono::system_clock::now().time_since_epoch()).count();
}

std::string Common::epochTimeMsToReadable(uint64_t epoch_time_ms) {
    auto tp = std::chrono::time_point<std::chrono::system_clock, std::chrono::milliseconds>(std::chrono::milliseconds(epoch_time_ms));
    auto tt = std::chrono::system_clock::to_time_t(tp);
    std::stringstream ss;
    ss << std::put_time(std::gmtime(&tt), "%Y-%m-%dT%H:%M:%S");
    ss << '.' << std::setfill('0') << std::setw(6) << (epoch_time_ms % 1000000);
    ss << "+00:00";
    return ss.str();
}

std::string Common::utcDateReadable() {
    return epochTimeMsToReadable(getEpochTimeMs());
}

std::string Common::getHostname() {
    std::string name;
    std::exception err = std::runtime_error("Error occured.");
    int returnCode = 0;
#if defined(WINDOWS)
    try
    {
        TCHAR  infoBuf[1024];
        DWORD  bufCharCount = 1024;
        if (!GetComputerName(infoBuf, &bufCharCount)) {
            returnCode = -1;
        }
        name = std::string(infoBuf);
    }
    catch (std::exception& ex)
    {
        err = ex;
        returnCode = -1;
    }
#else 
    char name_c[1024]{};
    int name_c_len = sizeof(name_c);
    returnCode = ::gethostname(name_c, name_c_len);
    name = std::string(name_c);
#endif
    if (returnCode != 0)
    {
        throw err;
    }
    return name;
}

bool Common::is_base64(unsigned char c) {
    return (isalnum(c) || (c == '+') || (c == '/'));
}

std::string Common::toBase64(std::string data) {
    const char* c_str = data.c_str();
    unsigned char const* bytes_to_encode = reinterpret_cast<const unsigned char*>(c_str);
    unsigned int in_len = (unsigned int)data.size();

    std::string ret;
    int i = 0;
    int j = 0;
    unsigned char char_array_3[3]{};
    unsigned char char_array_4[4]{};

    while (in_len--) {
        char_array_3[i++] = *(bytes_to_encode++);
        if (i == 3) {
            char_array_4[0] = (char_array_3[0] & 0xfc) >> 2;
            char_array_4[1] = ((char_array_3[0] & 0x03) << 4) + ((char_array_3[1] & 0xf0) >> 4);
            char_array_4[2] = ((char_array_3[1] & 0x0f) << 2) + ((char_array_3[2] & 0xc0) >> 6);
            char_array_4[3] = char_array_3[2] & 0x3f;

            for (i = 0; (i < 4); i++)
                ret += BASE64_CHARS[char_array_4[i]];
            i = 0;
        }
    }

    if (i)
    {
        for (j = i; j < 3; j++)
            char_array_3[j] = '\0';

        char_array_4[0] = (char_array_3[0] & 0xfc) >> 2;
        char_array_4[1] = ((char_array_3[0] & 0x03) << 4) + ((char_array_3[1] & 0xf0) >> 4);
        char_array_4[2] = ((char_array_3[1] & 0x0f) << 2) + ((char_array_3[2] & 0xc0) >> 6);
        char_array_4[3] = char_array_3[2] & 0x3f;

        for (j = 0; (j < i + 1); j++)
            ret += BASE64_CHARS[char_array_4[j]];

        while ((i++ < 3))
            ret += '=';

    }

    return ret;
}

std::string Common::fromBase64(std::string base64) {
    int in_len = (int)base64.size();
    int i = 0;
    int j = 0;
    int in_ = 0;
    unsigned char char_array_4[4]{};
    unsigned char char_array_3[3]{};
    std::string ret;

    while (in_len-- && (base64[in_] != '=') && is_base64(base64[in_])) {
        char_array_4[i++] = base64[in_]; in_++;
        if (i == 4) {
            for (i = 0; i < 4; i++)
                char_array_4[i] = (unsigned char)BASE64_CHARS.find(char_array_4[i]);

            char_array_3[0] = (char_array_4[0] << 2) + ((char_array_4[1] & 0x30) >> 4);
            char_array_3[1] = ((char_array_4[1] & 0xf) << 4) + ((char_array_4[2] & 0x3c) >> 2);
            char_array_3[2] = ((char_array_4[2] & 0x3) << 6) + char_array_4[3];

            for (i = 0; (i < 3); i++)
                ret += char_array_3[i];
            i = 0;
        }
    }

    if (i) {
        for (j = i; j < 4; j++)
            char_array_4[j] = 0;

        for (j = 0; j < 4; j++)
            char_array_4[j] = (unsigned char)BASE64_CHARS.find(char_array_4[j]);

        char_array_3[0] = (char_array_4[0] << 2) + ((char_array_4[1] & 0x30) >> 4);
        char_array_3[1] = ((char_array_4[1] & 0xf) << 4) + ((char_array_4[2] & 0x3c) >> 2);
        char_array_3[2] = ((char_array_4[2] & 0x3) << 6) + char_array_4[3];

        for (j = 0; (j < i - 1); j++) ret += char_array_3[j];
    }

    return ret;
}

std::string Common::readFile(std::filesystem::path filename) {
    std::ifstream inFile;
    inFile.open(filename);
    std::stringstream strStream;
    strStream << inFile.rdbuf();
    inFile.close();
    return strStream.str();
}

void Common::writeFile(std::filesystem::path filename, std::string content) {
    if (!std::filesystem::exists(filename.parent_path())) {
        std::filesystem::create_directories(filename.parent_path());
    }
    std::ofstream file(filename);
    file << content;
    file.close();
}

void Common::copyFile(std::filesystem::path source, std::filesystem::path dest, bool skipError) {
    if (!std::filesystem::exists(dest.parent_path())) {
        std::filesystem::create_directories(dest.parent_path());
    }
    if (skipError) {
        try {
            std::filesystem::copy(source, dest, std::filesystem::copy_options::overwrite_existing | std::filesystem::copy_options::recursive);
        }
        catch (...) { }
    }
    else {
        std::filesystem::copy(source, dest, std::filesystem::copy_options::overwrite_existing | std::filesystem::copy_options::recursive);
    }
}

void Common::copyAllFiles(std::filesystem::path sourceDir, std::filesystem::path destDir, std::vector<std::string> names, bool skipError) {
    for (const auto& entry : std::filesystem::directory_iterator(sourceDir)) {
        if (names.empty()) {
            copyFile(entry.path(), destDir / entry.path().filename(), skipError);
        }
        else {
            for (const auto& name : names) {
                if (name == entry.path().filename().string()) {
                    copyFile(entry.path(), destDir / entry.path().filename(), skipError);
                    break;
                }
            }
        }
    }
}

std::string Common::removeSpacesAndSpecialCharacter(std::string s) {
    s = std::regex_replace(s, std::regex("^ +| +$|( ) +"), "$1");
    s = std::regex_replace(s, std::regex("[^a-zA-Z0-9 ]"), "");
    return s;
}

std::string Common::strToLower(std::string s) {
    std::string newS = s;
    for (std::size_t i = 0; i < newS.size(); i++) {
        newS[i] = char(tolower(newS[i]));
    }
    return newS;
}

std::string Common::strToUpper(std::string s) {
    std::string newS = s;
    for (std::size_t i = 0; i < newS.size(); i++) {
        newS[i] = char(toupper(newS[i]));
    }
    return newS;
}

void Common::ltrim(std::string& s, std::optional<unsigned char> toTrim) {
    s.erase(s.begin(), std::find_if(s.begin(), s.end(), [&](unsigned char ch) {
        if (toTrim) {
            return ch != toTrim.value();
        }
        else {
            return !std::isspace(ch);
        }
        }));
}

void Common::rtrim(std::string& s, std::optional<unsigned char> toTrim) {
    s.erase(std::find_if(s.rbegin(), s.rend(), [&](unsigned char ch) {
        if (toTrim) {
            return ch != toTrim.value();
        }
        else {
            return !std::isspace(ch);
        }
        }).base(), s.end());
}

void Common::trim(std::string& s, std::optional<unsigned char> toTrim) {
    rtrim(s, toTrim);
    ltrim(s, toTrim);
}

std::string Common::ltrimCopy(std::string s, std::optional<unsigned char> toTrim) {
    ltrim(s, toTrim);
    return s;
}

std::string Common::rtrimCopy(std::string s, std::optional<unsigned char> toTrim) {
    rtrim(s, toTrim);
    return s;
}

std::string Common::trimCopy(std::string s, std::optional<unsigned char> toTrim) {
    trim(s, toTrim);
    return s;
}

std::vector<std::string> Common::splitStr(std::string s, std::vector<std::string> delimiters, bool removeEmpty) {
    std::vector<std::string> res;
    size_t pos_start = 0;
    std::string token;
    bool emptyToken = false;

    while (pos_start != std::string::npos) {
        size_t min_pos_end = std::string::npos;
        std::string min_delimiter;

        for (const auto& delimiter : delimiters) {
            size_t pos_end = s.find(delimiter, pos_start);
            if (pos_end != std::string::npos && pos_end < min_pos_end) {
                min_pos_end = pos_end;
                min_delimiter = delimiter;
            }
        }

        if (min_pos_end != std::string::npos) {
            token = s.substr(pos_start, min_pos_end - pos_start);
            pos_start = min_pos_end + min_delimiter.length();

            if (removeEmpty && token == "") {
                emptyToken = true;
                continue;
            }

            if (!emptyToken) {
                res.push_back(token);
            }
            else {
                emptyToken = false;
            }
        }
        else {
            token = s.substr(pos_start);

            if (removeEmpty && token == "") {
                return res;
            }

            res.push_back(token);
            break;
        }
    }

    return res;
}

std::string Common::joinStr(const std::vector<std::string>& lst, std::string delim, bool removeEmpty) {
    std::string ret;
    for (const auto& s : lst) {
        if (removeEmpty && s == "") {
            continue;
        }
        if (!ret.empty())
            ret += delim;
        ret += s;
    }
    return ret;
}

std::string Common::removeStr(std::string s, std::vector<std::string> toRemove, bool caseSensitive) {
    std::string newS = s;
    for (auto& toR : toRemove) {
        std::size_t ind = 0;
        while (ind != std::string::npos) {
            if (caseSensitive) {
                ind = newS.find(toR);
            }
            else {
                ind = strToLower(newS).find(strToLower(toR));
            }
            if (ind != std::string::npos) {
                newS.erase(ind, toR.length());
            }
        }
    }
    return newS;
}

std::string Common::replaceStr(std::string s, const std::string& from, const std::string& to, bool caseSensitive) {
    if (from.empty()) {
        return s;
    }
    std::string newS = s;
    std::size_t ind = 0;
    while (ind != std::string::npos) {
        if (caseSensitive) {
            ind = newS.find(from, ind);
        }
        else {
            ind = strToLower(newS).find(strToLower(from), ind);
        }
        if (ind != std::string::npos) {
            newS.replace(ind, from.length(), to);
            ind += to.length();
        }
    }
    return newS;
}

bool Common::startsWith(std::string s, std::string start) {
    if (s.length() < start.length()) {
        return false;
    }
    return s.substr(0, start.length()) == start;
}

bool Common::endsWith(std::string s, std::string end) {
    if (s.length() < end.length()) {
        return false;
    }
    return s.substr(s.length() - end.length()) == end;
}

std::string Common::randomUuid() {
    return boost::lexical_cast<std::string>(boost::uuids::random_generator()());
}
