import std.stdio:writeln;
import std.file;
import std.conv;
import std.string;
import std.math;
import std.algorithm;
import std.regex;

void wavesToDrm(string path, string outputPath)
{
	union SampleData
	{
		int* channelData;
		short* dataSet;
	}

    uint noteDur = 44100*3; // (...) 3 segs (samples).
	string numPartPattern = r"\d+(?=.*\.\w+$)";
	int totalFiles;
	int[] drm;
	int[] smps;
	void[] smpsB;
	SampleData smpData;
	float amp = 1f;
	float fade0 = 0.75f;
	float fade1 = 1f;

	// Percorre os subdiretórios da pasta de samples.
	foreach (DirEntry entry; dirEntries(path, SpanMode.shallow))
	{
		if (entry.isDir() && std.path.baseName(entry.name) == "GrandPiano")
		{
			string[] audioNames;

			// Percorre os samples (arquivos) em cada pasta.
			foreach (DirEntry audioEntry; dirEntries(entry.name, "*.wav", SpanMode.shallow))
				version (Windows)
				{
					// Se não for um arquivo oculto.
					if (!(audioEntry.attributes & 2))
						audioNames ~= audioEntry.name;
				}
				else audioNames ~= audioEntry.name;

			// Ordena os arquivos por número.
			sort!(delegate bool(string a, string b)
			{
				return to!int(match(a, numPartPattern).hit) < to!int(match(b, numPartPattern).hit);
			})(audioNames);

			totalFiles = audioNames.length;
			drm = new int[totalFiles*noteDur];

			for (int j = 0; j < totalFiles; ++j)
			{
				// Lê o wave sem o cabeçalho.
				smpsB = read(audioNames[j])[44..$];
				smpsB ~= new void[smpsB.length%4];
				smps = cast(int[])smpsB;

				if (smps.length < noteDur)
					smps ~= (new int[noteDur - smps.length])[] = 0;

				int totalSamples = smps.length;

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
				drm[j*noteDur..(j + 1)*noteDur] = smps[0..noteDur];

				delete smps;
			}

			write(text(outputPath, "/", std.path.baseName(entry.name), ".drm"), drm);

			delete drm;
			delete audioNames;
		}
	}
}

void drmToWave(string path)
{
	const uint SAMPLING_RATE = 44100;
	const uint NUM_CHANNELS = 2;
	const uint BITS_SAMPLE = 16;

	ubyte[] h;
	int[] drm = cast(int[])read(path);
	uint tData = drm.length*4;

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

	write(path[0..$ - 3] ~ "wav", h ~ cast(ubyte[])drm);

	delete h;
	delete drm;
}

ubyte[] intToLEBytes(uint n, uint size)
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
	wavesToDrm("audio", "output");
	//drmToWave("output/Mixed.drm");

    return 0;
}
