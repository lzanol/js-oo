#include <jni.h>
#include <string>

extern "C" {

JNIEXPORT jstring JNICALL
Java_com_lzanol_chords_MainActivity_initEngine(
        JNIEnv *env,
        jobject /* this */) {
    return env->NewStringUTF("Hello!!");
}

}
