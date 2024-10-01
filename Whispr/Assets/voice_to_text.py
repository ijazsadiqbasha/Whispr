import os
import numpy as np
from faster_whisper import WhisperModel
from transformers import WhisperProcessor, WhisperForConditionalGeneration
import ctranslate2

def download_model(model_name, cache_dir):
    try:
        os.makedirs(cache_dir, exist_ok=True)

        # Load HF model
        hf_model = WhisperForConditionalGeneration.from_pretrained(model_name, cache_dir=cache_dir)

        # Prepare CTranslate2 directory for the converted model
        ct2_dir = os.path.join(cache_dir, f"{model_name.replace('/', '_')}_ct2")

        # Convert Hugging Face model to CTranslate2 format if it doesn't already exist
        if not os.path.exists(ct2_dir):
            converter = ctranslate2.converters.TransformersConverter(model_name)
            converter.convert(ct2_dir, quantization="float32")

            # Remove the models-- folder
            models_folder = os.path.join(cache_dir, f"models--{model_name.replace('/', '--')}")
            if os.path.exists(models_folder):
                for root, dirs, files in os.walk(models_folder, topdown=False):
                    for file in files:
                        os.remove(os.path.join(root, file))
                    for dir in dirs:
                        os.rmdir(os.path.join(root, dir))
                os.rmdir(models_folder)

        return "Model downloaded and converted successfully"
    except Exception as e:
        raise

def load_model(model_name, cache_dir):
    try:
        # Prepare CTranslate2 directory for the converted model
        ct2_dir = os.path.join(cache_dir, f"{model_name.replace('/', '_')}_ct2")

        # Load the model using Faster-Whisper with float32
        model = WhisperModel(ct2_dir, device="cpu", compute_type="float32")

        return model
    except Exception as e:
        raise

def transcribe(audio_data, loadded_model, progress_callback=None):
    try:
        # Convert audio_data to numpy array
        audio_np = np.frombuffer(audio_data, dtype=np.int16).astype(np.float32) / 32768.0

        # Transcribe using the model
        segments, info = loadded_model.transcribe(audio_np, beam_size=1, best_of=1, temperature=0.0, language="en")

        transcription = []
        segment_count = 0  # Track number of segments processed

        # Iterate through the segments
        for segment in segments:
            transcription.append(segment.text)
            segment_count += 1

            # Invoke the progress callback with a proportional value
            if progress_callback:
                # Fake progress based on the number of segments processed
                # Assuming a maximum of 100 segments for progress tracking purposes
                fake_progress = min(segment_count / 100, 1.0)  # Cap at 1.0
                progress_callback(fake_progress)

        # Combine all segments into a single string
        return " ".join(transcription)
    except Exception as e:
        raise
