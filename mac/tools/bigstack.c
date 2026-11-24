/* DYLD interposer: give every new thread a large stack.
 * Cemu 2.6 on macOS crashes (Bus error 10) inside MoltenVK's SPIRV-Cross when it converts the deeply nested
 * shader of the Wii U N64 Virtual Console emulator: the recursion overflows the default 512 KB thread stack. */
#include <pthread.h>
#include <stddef.h>

#define BIG_STACK ((size_t)128 * 1024 * 1024)

static int big_pthread_create(pthread_t *t, const pthread_attr_t *attr, void *(*fn)(void *), void *arg)
{
	pthread_attr_t a;
	size_t size = 0;
	if (attr) a = *attr; else pthread_attr_init(&a);
	pthread_attr_getstacksize(&a, &size);
	if (size < BIG_STACK) pthread_attr_setstacksize(&a, BIG_STACK);
	return pthread_create(t, &a, fn, arg);
}

__attribute__((used)) static struct { const void *replacement; const void *original; } interposers[]
	__attribute__((section("__DATA,__interpose"))) = { { (const void *)big_pthread_create, (const void *)pthread_create } };
