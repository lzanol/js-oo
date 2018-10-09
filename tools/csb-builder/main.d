import std.stdio:writeln;
import std.path:baseName;
import std.file;
import std.conv;
import std.string;
import std.math;
import std.algorithm;
import std.regex;

void wavesToRaw(string path, string outputPath) {
	union SampleData {
		int* channelData;
		short* dataSet;
	}

	const uint SAMPLING_RATE = 48000;

    uint noteDur = SAMPLING_RATE*4; // secs in samples
	string numPartPattern = r"\d+(?=.*\.\w+$)";
	ulong totalFiles;
	int[] raw;
	int[] smps;
	void[] smpsB;
	SampleData smpData;
	float amp = 1f;
	float fade0 = 0.75f;
	float fade1 = 1f;

	// look for subdirs
	foreach (DirEntry entry; dirEntries(path, SpanMode.shallow)) {
		if (!entry.isDir())
			continue;

		string[] audioNames;

		// reads each sample of current subdir
		foreach (DirEntry audioEntry; dirEntries(entry.name, "*.wav", SpanMode.shallow)) {
			version (Windows) {
				// ignores hidden files (Windows)
				if (!(audioEntry.attributes & 2))
					audioNames ~= audioEntry.name;
			} else audioNames ~= audioEntry.name;
		}

		sort!()(audioNames);

		writeln("Concatenating...");
		writeln(audioNames);

		totalFiles = audioNames.length;
		raw = new int[totalFiles*noteDur];

		for (int j = 0; j < totalFiles; ++j) {
			// reading wave (skiping header)
			smpsB = read(audioNames[j])[44..$];
			// normalizing array size
			smpsB ~= new void[smpsB.length%4];
			smps = cast(int[])smpsB;

			// fills with silence note duration is less
			if (smps.length < noteDur)
				smps ~= (new int[noteDur - smps.length])[] = 0;

			ulong totalSamples = smps.length;

			// Amplitude
			for (int k = 0; k < totalSamples; ++k)
			{
				smpData.channelData = &smps[k];
				*smpData.dataSet *= amp;
				*(smpData.dataSet + 1) *= amp;
			}

			int fadePos0 = cast(int)(totalSamples*fade0);
			int fadePos1 = cast(int)(totalSamples*fade1);
			double totalFadeSamples = cast(double)(fadePos1 - fadePos0);
			double mult;

			// Fade-out
			for (int k = fadePos0; k < totalSamples; ++k)
			{
				if (k < fadePos1)
					mult = 1f - (k - fadePos0)/totalFadeSamples;
				else mult = 0;

				smpData.channelData = &smps[k];
				*smpData.dataSet *= mult;
				*(smpData.dataSet + 1) *= mult;
			}

			// (...) Remove a assinatura da Sony (Sound Forge).
			//smps[$ - 44..$] = 0;

			// Corta o sample se exceder ou preenche com silêncio se faltar.
			raw[j*noteDur..(j + 1)*noteDur] = smps[0..noteDur];

			smps.destroy();
		}

		write(text(outputPath, "/", baseName(entry.name), ".csb"), raw);

		raw.destroy();
		audioNames.destroy();
	}
}

void rawToWave(string path)
{
	const uint SAMPLING_RATE = 44100;
	const uint NUM_CHANNELS = 2;
	const uint BITS_SAMPLE = 16;

	ubyte[] h;
	auto raw = cast(ubyte[])read(path);
	const tData = raw.length;

	h ~= "RIFF"; // ChunkID
	h ~= intToLEBytes(tData + 36, 4); // ChunkSize
	h ~= "WAVE"; // Format
	h ~= "fmt "; // Subchunk1ID
	h ~= intToLEBytes(16, 4); // Subchunk1Size
	h ~= intToLEBytes(1, 2); // AudioFormat
	h ~= intToLEBytes(NUM_CHANNELS, 2); // NumChannels
	h ~= intToLEBytes(SAMPLING_RATE, 4); // SampleRate
	h ~= intToLEBytes(SAMPLING_RATE*NUM_CHANNELS*BITS_SAMPLE/8, 4); // ByteRate
	h ~= intToLEBytes(NUM_CHANNELS*BITS_SAMPLE/8, 2); // BlockAlign
	h ~= intToLEBytes(BITS_SAMPLE, 2); // BitsPerSample
	h ~= "data"; // Subchunk2ID
	h ~= intToLEBytes(tData, 4); // Subchunk2Size

	write(path[0..$ - 3] ~ "wav", h ~ raw);

	h.destroy();
	raw.destroy();
}

ubyte[] intToLEBytes(ulong n, uint size)
{
	ubyte[] nb;

	nb.length = size;

	// Em "x86" os números já são armazenados em "little endian".
	version (X86)
	{
		ubyte* bp = cast(ubyte*)&n;
		nb = bp[0..size].dup;
	}
	else
	{
		for (int i = 0; i < size; ++i)
		{
			nb[i] = 255 & n;
			n >>= 8;
		}
	}

	return nb;
}

int main(char[][] args)
{
	wavesToRaw("input", "output");
	//rawToWave("output/Mixed.drm");

    return 0;
}
