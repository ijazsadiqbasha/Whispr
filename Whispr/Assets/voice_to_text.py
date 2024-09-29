import os
import numpy as np
import ctranslate2
from faster_whisper import WhisperModel
from transformers import WhisperForConditionalGeneration, WhisperProcessor

# Initialize model and VAD globally
model = None

def download_model(model_name, cache_dir):
    # Download the model
    model = WhisperForConditionalGeneration.from_pretrained(model_name, cache_dir=cache_dir)
    # processor = WhisperProcessor.from_pretrained(model_name, cache_dir=cache_dir)

def convert_model(model_name, cache_dir):
    # Convert the model using ctranslate2
    output_dir = os.path.join(cache_dir, model_name.replace("/", "_"))
    converter = ctranslate2.converters.TransformersConverter(model_name)
    converter.convert(output_dir, quantization="int8")

    # Remove the models-- folder
    models_folder = os.path.join(cache_dir, f"models--{model_name.replace('/', '--')}")
    if os.path.exists(models_folder):
        for root, dirs, files in os.walk(models_folder, topdown=False):
            for file in files:
                os.remove(os.path.join(root, file))
            for dir in dirs:
                os.rmdir(os.path.join(root, dir))
        os.rmdir(models_folder)

def load_model(model_name, cache_dir):
    # Load the model
    global model
    model_path = os.path.join(cache_dir, model_name.replace("/", "_"))
    model = WhisperModel(model_path, device="cpu", compute_type="int8")

def transcribe(audio_data):
    # Assuming audio_data is a byte array from C#
    # Convert byte array to numpy array
    audio_np = np.frombuffer(audio_data, dtype=np.float32)  # Assuming float32 format
    result = model.transcribe(audio_np)
    return result