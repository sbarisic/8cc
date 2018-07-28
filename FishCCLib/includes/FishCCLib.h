#pragma once

const char* csharp_clean(const char*);
int csharp_strncasecmp(const char* string1, const char* string2, int count);
int csharp_strcasecmp(const char* string1, const char* string2);

char* csharp_dirname(char* path);
char* csharp_basename(char* path);

int csharp_mkstemps(char *template, int suffixlen);