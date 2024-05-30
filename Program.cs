using ConsoleApp1;
using OpenAI_API;
using OpenAI_API.Chat;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace TelegramBot
{
    class Program
    {
        private static ITelegramBotClient botClient;
        private static OpenAIAPI openAiClient;
        private static ConcurrentDictionary<long, UserState> userStates = new();

        static async Task Main()
        {
            try
            {
                //Tokens Init

                using var cts = new CancellationTokenSource();

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = { }
                };

                botClient.StartReceiving(
                    HandleUpdateAsync,
                    HandleErrorAsync,
                    receiverOptions,
                    cancellationToken: cts.Token
                );

                var me = await botClient.GetMeAsync();
                Console.WriteLine($"Start listening for @{me.Username}");
                Console.ReadLine();

                cts.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Main: " + ex);
            }
        }

        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message)
                {
                    var chatId = update.Message!.Chat.Id;

                    if (update.Message.Type == MessageType.Text)
                    {
                        var messageText = update.Message.Text.ToLower();

                        if (messageText == "/start")
                        {
                            var user = update.Message.From;
                            var userName = user.Username ?? user.FirstName;
                            var welcomeMessage = $"Hello {userName}! I am your car insurance assistant bot. Please submit a photo of your passport.";
                            await botClient.SendTextMessageAsync(chatId, welcomeMessage);

                            userStates[chatId] = UserState.WaitingForPassportPhoto;
                        }
                        else if (userStates.TryGetValue(chatId, out var state))
                        {
                            switch (state)
                            {
                                case UserState.WaitingForPassportConfirmation:
                                    if (messageText == "yes")
                                    {
                                        await botClient.SendTextMessageAsync(chatId, "Please submit a photo of your tech passport.");
                                        userStates[chatId] = UserState.WaitingForTechPassportPhoto;
                                    }
                                    else if (messageText == "no")
                                    {
                                        await botClient.SendTextMessageAsync(chatId, "Please retake and submit the photo of your passport.");
                                        userStates[chatId] = UserState.WaitingForPassportPhoto;
                                    }
                                    break;

                                case UserState.WaitingForTechPassportConfirmation:
                                    if (messageText == "yes")
                                    {
                                        await botClient.SendTextMessageAsync(chatId, "The fixed price for the insurance is 100 USD. Do you agree with the price? (agree/disagree)");
                                        userStates[chatId] = UserState.WaitingForPriceConfirmation;
                                    }
                                    else if (messageText == "no")
                                    {
                                        await botClient.SendTextMessageAsync(chatId, "Please retake and submit the photo of your tech passport.");
                                        userStates[chatId] = UserState.WaitingForTechPassportPhoto;
                                    }
                                    break;

                                case UserState.WaitingForPriceConfirmation:
                                    if (messageText == "agree")
                                    {
                                        await botClient.SendTextMessageAsync(chatId, "Thank you for your purchase! Here is your insurance policy.");

                                        var policyDocument = GeneratePolicyDocument();
                                        await botClient.SendDocumentAsync(chatId, new InputOnlineFile(new MemoryStream(policyDocument), "policy.txt"));
                                        userStates[chatId] = UserState.Completed;
                                    }
                                    else if (messageText == "disagree")
                                    {
                                        await botClient.SendTextMessageAsync(chatId, "Sorry, 100 USD is the only available price.");
                                    }
                                    break;

                                default:
                                    var aiResponse = await GetAiResponse(messageText);
                                    await botClient.SendTextMessageAsync(chatId, aiResponse);
                                    break;
                            }
                        }
                    }
                    else if (update.Message.Type == MessageType.Photo && userStates.TryGetValue(chatId, out var state))
                    {
                        var photo = update.Message.Photo[^1]; // Get the highest resolution photo

                        switch (state)
                        {
                            case UserState.WaitingForPassportPhoto:
                                // Simulate data extraction from passport photo
                                var passportData = SimulatePassportDataExtraction();
                                await botClient.SendTextMessageAsync(chatId, $"Passport Data: {passportData}. Is this correct? (yes/no)");
                                userStates[chatId] = UserState.WaitingForPassportConfirmation;
                                break;

                            case UserState.WaitingForTechPassportPhoto:
                                // Simulate data extraction from tech passport photo
                                var techPassportData = SimulateTechPassportDataExtraction();
                                await botClient.SendTextMessageAsync(chatId, $"Tech Passport Data: {techPassportData}. Is this correct? (yes/no)");
                                userStates[chatId] = UserState.WaitingForTechPassportConfirmation;
                                break;

                            case UserState.Completed:
                                await botClient.SendTextMessageAsync(chatId, "You have already submitted both documents. Type /start to restart the process.");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(botClient, ex, cancellationToken);
            }
        }

        static string SimulatePassportDataExtraction()
        {
            string name = "John Doe";
            string passNo = "123456789";
            return $"Name: {name}, Passport No: {passNo}";
        }

        static string SimulateTechPassportDataExtraction()
        {
            string carModel = "Toyota Camry";
            string regNo = "ABC1234";
            return $"Car Model: {carModel}, Registration No: {regNo}";
        }

        static byte[] GeneratePolicyDocument()
        {
            try
            {
                // Simulate generating a dummy insurance policy document (TXT)
                var policyContent = "This is a dummy insurance policy for John Doe.";
                return Encoding.UTF8.GetBytes(policyContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GeneratePolicyDocument: " + ex);
                return null;
            }
        }

        static async Task<string> GetAiResponse(string prompt)
        {
            try
            {
                var completionRequest = new OpenAI_API.Completions.CompletionRequest
                {
                    Prompt = prompt,
                    MaxTokens = 150
                };

                var completionResult = await openAiClient.Completions.CreateCompletionAsync(completionRequest);
                return completionResult.Completions[0].Text.Trim();
            }
            catch (HttpRequestException httpEx)
            {
                if (httpEx.Message.Contains("insufficient_quota"))
                {
                    Console.WriteLine("GetAiResponse: Insufficient quota.");
                    return "Sorry, I've reached my usage limit for the day. Please try again later or contact support.";
                }
                else
                {
                    Console.WriteLine("GetAiResponse: " + httpEx);
                    return "Sorry, I couldn't process your request due to a network error.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAiResponse: " + ex);
                return "Sorry, an unexpected error occurred while processing your request.";
            }
        }

        static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            try
            {
                var errorMessage = exception switch
                {
                    ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };

                Console.WriteLine(errorMessage);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling exception: {ex}");
                return Task.CompletedTask;
            }
        }
    }

    enum UserState
    {
        WaitingForPassportPhoto,
        WaitingForPassportConfirmation,
        WaitingForTechPassportPhoto,
        WaitingForTechPassportConfirmation,
        WaitingForPriceConfirmation,
        Completed
    }
}