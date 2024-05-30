# Telegram Car Insurance Bot

## Overview
This Telegram bot assists users in purchasing car insurance by processing user-submitted documents, interacting through AI-driven communications, and confirming transaction details.

## Features
1. **Bot Setup**:
   - Initializes a Telegram bot using the Telegram Bot API.
   - Introduces itself and explains its purpose when a user starts a conversation.

2. **Document Submission**:
   - Prompts the user to submit a photo of their passport and vehicle identification document.

3. **Data Extraction and Confirmation**:
   - Simulates data extraction from the submitted photos.
   - Displays the extracted data to the user for confirmation.
   - If the user disagrees with the extracted data, requests resubmission.

4. **Price Quotation**:
   - Informs the user that the fixed price for the insurance is 100 USD.
   - Asks the user if they agree with the price.

5. **Insurance Policy Issuance**:
   - Generates a dummy insurance policy document using a pre-formatted text.
   - Sends the dummy policy to the user as confirmation of purchase.

## Setup Instructions

### Prerequisites
- .NET Core SDK
- Telegram Bot Token (obtained from BotFather on Telegram)
- OpenAI API Key (if you want to use OpenAI for generating the policy text)

### Installation / run project
1. Clone the repository:
   git bash
   git clone https://github.com/TarasOheruk/CarTGBot

2. Install necessary packages:
	dotnet add package Telegram.Bot

3. Replace YOUR_BOT_TOKEN in the code with your actual tokens.
 
4. dotnet run

### Workflow
Start the Bot: User sends /start command.

Bot introduces itself and asks for a photo of the passport.
	Submit Photo: User submits a photo of the passport.

Bot simulates data extraction and asks for confirmation.
	Data Confirmation: User confirms the extracted data command(yes/no).

Bot quotes a fixed price of 100 USD for the insurance.
	Price Agreement: User agrees to the price ommand(agree/disagree).

Bot generates and sends a dummy insurance policy document.
	Completion: User receives the insurance policy document.