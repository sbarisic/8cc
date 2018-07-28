#include "..\8cc.h"

#include <string.h>

int strncasecmp(const char *string1, const char *string2, size_t count) {
	return strncmp(string1, string2, count);
}

int strcasecmp(const char *string1, const char *string2) {
	return strcmp(string1, string2);
}

char *dirname(char *path) {
	return NULL;
}

char *basename(char *path) {
	return NULL;
}

int mkstemps(char *template, int suffixlen) {
	return 0;
}

struct tm *localtime_r(const time_t *timep, struct tm *result) {
	return NULL;
}