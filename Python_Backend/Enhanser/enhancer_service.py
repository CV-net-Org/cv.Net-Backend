import os
from openai import OpenAI
from dotenv import load_dotenv

load_dotenv()

client = OpenAI(
    base_url="https://integrate.api.nvidia.com/v1",
    api_key=os.getenv("API_KEY") # Ensure this is in your .env
)

def enhance_text_with_ai(input_text, mode, custom_prompt=None):
    """
    Orchestrates the AI response based on the requested mode.
    """
    
    # 1. Define the System Instructions based on the "Mode"
    if mode == "summarize":
        system_msg = "You are a professional editor. Summarize the user's input into clear, concise bullet points."
    elif mode == "formalize":
        system_msg = "You are a career consultant. Rewrite the user's input in a highly professional, formal tone with perfect grammar. Keep the original meaning but make it sound executive-level."
    elif mode == "custom":
        system_msg = f"You are a helpful assistant. Follow this specific instruction: {custom_prompt}"
    else:
        system_msg = "You are a professional writing assistant."

    try:
        response = client.chat.completions.create(
            model="mistralai/mistral-nemotron",
            messages=[
                {"role": "system", "content": system_msg},
                {"role": "user", "content": input_text}
            ],
            temperature=0.6,
            top_p=0.7,
            max_tokens=4096,
            stream=False # Set to False for a quick JSON response in Postman
        )
        
        return response.choices[0].message.content
    except Exception as e:
        return f"AI Error: {str(e)}"