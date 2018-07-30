extern void Exit();
extern void Write(const char* Str);
extern void WriteLine(const char* Str);
//extern void copyfunc(unsigned long func_size, void* func);

int main(int argc, const char** argv) {
	Write("Hello");
	Write(" ");
	WriteLine("World!");

	Exit();
	return 0;
}