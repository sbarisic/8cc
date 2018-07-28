//#include "..\8cc.h"

#include <string.h>
#include <FishCCLib.h>

//#include <Windows.h>
#include <time.h>
#include <io.h>

int strncasecmp(const char *string1, const char *string2, size_t count) {
	return csharp_strncasecmp(string1, string2, count);
}

int strcasecmp(const char *string1, const char *string2) {
	return csharp_strcasecmp(string1, string2);
}

char *dirname(char *path) {
	return csharp_dirname(path);
}

char *basename(char *path) {
	return csharp_basename(path);
}

int mkstemps(char *template, int suffixlen) {
	return csharp_mkstemps(template, suffixlen);
}

struct tm *localtime_r(const time_t *timep, struct tm *result) {
	*result = *localtime(timep);
	return result;
}