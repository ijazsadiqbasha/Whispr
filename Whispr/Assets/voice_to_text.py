import os
import numpy as np
import logging
from faster_whisper import WhisperModel
from transformers import WhisperProcessor, WhisperForConditionalGeneration
import ctranslate2

# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

def download_model(model_name, cache_dir, progress_callback=None):
    try:
        os.makedirs(cache_dir, exist_ok=True)

        if progress_callback:
            progress_callback(0)

        logger.info(f"Downloading model '{model_name}'...")

        # Load processor and HF model
        processor = WhisperProcessor.from_pretrained(model_name, cache_dir=cache_dir)
        hf_model = WhisperForConditionalGeneration.from_pretrained(model_name, cache_dir=cache_dir)

        if progress_callback:
            progress_callback(55)

        # Prepare CTranslate2 directory for the converted model
        ct2_dir = os.path.join(cache_dir, f"{model_name.replace('/', '_')}_ct2")

        # Convert Hugging Face model to CTranslate2 format if it doesn't already exist
        if not os.path.exists(ct2_dir):
            logger.info("Converting model to CTranslate2 format...")
            converter = ctranslate2.converters.TransformersConverter(model_name)
            converter.convert(ct2_dir, quantization="float32")
            logger.info(f"Model converted and saved to {ct2_dir}")

            # Remove the models-- folder
            models_folder = os.path.join(cache_dir, f"models--{model_name.replace('/', '--')}")
            if os.path.exists(models_folder):
                for root, dirs, files in os.walk(models_folder, topdown=False):
                    for file in files:
                        os.remove(os.path.join(root, file))
                    for dir in dirs:
                        os.rmdir(os.path.join(root, dir))
                os.rmdir(models_folder)
        else:
            logger.info(f"Using cached model from {ct2_dir}")

        if progress_callback:
            progress_callback(100)

        logger.info(f"Model '{model_name}' downloaded and converted successfully.")
        return "Model downloaded and converted successfully"
    except Exception as e:
        logger.error(f"Error downloading/converting model '{model_name}': {str(e)}", exc_info=True)
        raise

def load_processor(model_name, cache_dir, progress_callback=None):
    try:
        if progress_callback:
            progress_callback(0)

        logger.info(f"Loading Whisper processor '{model_name}'...")
        processor = WhisperProcessor.from_pretrained(model_name, cache_dir=cache_dir)

        if progress_callback:
            progress_callback(100)

        logger.info(f"Processor '{model_name}' loaded successfully.")
        return processor
    except Exception as e:
        logger.error(f"Error loading processor '{model_name}': {str(e)}", exc_info=True)
        raise

def load_model(model_name, cache_dir, progress_callback=None):
    try:
        if progress_callback:
            progress_callback(0)

        logger.info(f"Loading Faster-Whisper model '{model_name}'...")

        # Prepare CTranslate2 directory for the converted model
        ct2_dir = os.path.join(cache_dir, f"{model_name.replace('/', '_')}_ct2")

        # Check if the model exists, if not, download and convert it
        if not os.path.exists(ct2_dir):
            logger.info(f"Model not found. Downloading and converting '{model_name}'...")
            download_model(model_name, cache_dir, progress_callback)

        # Load the model using Faster-Whisper with float32
        model = WhisperModel(ct2_dir, device="cpu", compute_type="float32")

        if progress_callback:
            progress_callback(100)

        logger.info(f"Model '{model_name}' loaded successfully.")
        return model
    except Exception as e:
        logger.error(f"Error loading model '{model_name}': {str(e)}", exc_info=True)
        raise

def transcribe(audio_data, model, progress_callback=None):
    try:
        if progress_callback:
            progress_callback(0)

        # Convert audio_data to numpy array
        audio_np = np.frombuffer(audio_data, dtype=np.int16).astype(np.float32) / 32768.0

        if progress_callback:
            progress_callback(50)

        # Transcribe using the model
        segments, info = model.transcribe(audio_np, beam_size=5)

        if progress_callback:
            progress_callback(75)

        # Combine all segments into a single string
        transcription = " ".join(segment.text for segment in segments)

        if progress_callback:
            progress_callback(100)

        return transcription
    except Exception as e:
        logger.error(f"Error transcribing audio: {str(e)}", exc_info=True)
        raise
