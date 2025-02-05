#if defined(WINDOWS)

#include "application/common.h"

bool Common::isElevated() {
    BOOL fRet = FALSE;
    HANDLE hToken = NULL;
    if (OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken)) {
        TOKEN_ELEVATION Elevation{};
        DWORD cbSize = (DWORD)sizeof(TOKEN_ELEVATION);
        if (GetTokenInformation(hToken, TokenElevation, &Elevation, sizeof(Elevation), &cbSize)) {
            fRet = Elevation.TokenIsElevated;
        }
    }
    if (hToken) {
        CloseHandle(hToken);
    }
    return fRet;
}

std::string Common::getStrFromRegistry(std::string keyPath) {
    HKEY hKey;
    HKEY pathKey{};

    std::vector<std::string> keyPathSplit = Common::splitStr(keyPath, { "\\" });

    if (keyPathSplit[0] == "HKEY_CLASSES_ROOT") {
        pathKey = HKEY_CLASSES_ROOT;
    }
    else if (keyPathSplit[0] == "HKEY_CURRENT_USER") {
        pathKey = HKEY_CURRENT_USER;
    }
    else if (keyPathSplit[0] == "HKEY_LOCAL_MACHINE") {
        pathKey = HKEY_LOCAL_MACHINE;
    }
    else if (keyPathSplit[0] == "HKEY_USERS") {
        pathKey = HKEY_USERS;
    }
    else if (keyPathSplit[0] == "HKEY_CURRENT_CONFIG") {
        pathKey = HKEY_CURRENT_CONFIG;
    }
    else {
        throw new std::runtime_error("Failed to open the registry key " + keyPath);
    }

    const char* valueKey = keyPathSplit[keyPathSplit.size() - 1].c_str();

    std::string subKey = "";
    for (std::vector<std::string>::const_iterator p = keyPathSplit.begin() + 1; p != keyPathSplit.end() - 1; ++p) {
        subKey += *p;
        if (p != keyPathSplit.end() - 2)
            subKey += "\\";
    }

    std::string value;

    if (RegOpenKeyExA(pathKey, subKey.c_str(), 0, KEY_QUERY_VALUE, &hKey) == ERROR_SUCCESS) {
        DWORD bufferSize = 0;
        DWORD valueType;

        if (RegQueryValueExA(hKey, valueKey, nullptr, &valueType, nullptr, &bufferSize) == ERROR_SUCCESS) {
            if (valueType == REG_SZ) {
                char* valueBuffer = new char[bufferSize];

                if (RegQueryValueExA(hKey, valueKey, nullptr, nullptr, reinterpret_cast<LPBYTE>(valueBuffer), &bufferSize) == ERROR_SUCCESS) {
                    value = std::string(valueBuffer);
                }

                delete[] valueBuffer;
            }
        }

        RegCloseKey(hKey);
    }
    else {
        throw new std::runtime_error("Failed to open the registry key " + keyPath);
    }

    return value;
}

void Common::setStrFromRegistry(std::string keyPath, std::string value) {
    HKEY hKey;
    HKEY pathKey{};

    std::vector<std::string> keyPathSplit = Common::splitStr(keyPath, { "\\" });

    if (keyPathSplit[0] == "HKEY_CLASSES_ROOT") {
        pathKey = HKEY_CLASSES_ROOT;
    }
    else if (keyPathSplit[0] == "HKEY_CURRENT_USER") {
        pathKey = HKEY_CURRENT_USER;
    }
    else if (keyPathSplit[0] == "HKEY_LOCAL_MACHINE") {
        pathKey = HKEY_LOCAL_MACHINE;
    }
    else if (keyPathSplit[0] == "HKEY_USERS") {
        pathKey = HKEY_USERS;
    }
    else if (keyPathSplit[0] == "HKEY_CURRENT_CONFIG") {
        pathKey = HKEY_CURRENT_CONFIG;
    }
    else {
        throw new std::runtime_error("Failed to open the registry key " + keyPath);
    }

    const char* valueKey = keyPathSplit[keyPathSplit.size() - 1].c_str();

    std::string subKey = "";
    for (std::vector<std::string>::const_iterator p = keyPathSplit.begin() + 1; p != keyPathSplit.end() - 1; ++p) {
        subKey += *p;
        if (p != keyPathSplit.end() - 2)
            subKey += "\\";
    }

    if (RegOpenKeyExA(pathKey, subKey.c_str(), 0, KEY_SET_VALUE, &hKey) == ERROR_SUCCESS) {
        LPCTSTR data = value.c_str();

        if (RegSetValueExA(hKey, valueKey, 0, REG_SZ, (LPBYTE)data, (DWORD)strlen(data) + 1) != ERROR_SUCCESS) {
            throw new std::runtime_error("Failed to set the registry value " + keyPath);
        }

        RegCloseKey(hKey);
    }
    else {
        throw new std::runtime_error("Failed to open the registry key " + keyPath);
    }
}

std::string Common::bstrToString(BSTR bstr) {
    if (!bstr) return "";

    int bstrLen = SysStringLen(bstr);
    if (bstrLen == 0) return "";

    int utf8Len = WideCharToMultiByte(CP_UTF8, 0, bstr, bstrLen, nullptr, 0, nullptr, nullptr);
    if (utf8Len == 0) {
        return "";
    }

    std::string result(utf8Len, '\0');
    WideCharToMultiByte(CP_UTF8, 0, bstr, bstrLen, &result[0], utf8Len, nullptr, nullptr);

    return result;
}

#endif
