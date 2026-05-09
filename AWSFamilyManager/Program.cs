// See https://aka.ms/new-console-template for more information
using AWSFamilyManager;

Console.WriteLine("Starting setup amazon family.");

AWSFamilySetup familySetup = new AWSFamilySetup();

await familySetup.SetupFamily();

