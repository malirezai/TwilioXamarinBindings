#addin nuget:?package=Cake.XCode
#addin nuget:?package=Cake.Xamarin
#addin nuget:?package=Cake.FileHelpers

// Video - Rooms
// https://www.twilio.com/docs/api/video/download-video-sdks
// https://bintray.com/twilio/releases/download_file?file_path=com%2Ftwilio%2Fvideo-android%2F1.0.0-beta3%2Fvideo-android-1.0.0-beta3.aar
// https://media.twiliocdn.com/sdk/ios/video/latest/twilio-video-ios.tar.bz2
// https://www.twilio.com/docs/api/video/quickstart-sample-apps

// https://github.com/twilio/video-quickstart-android/tree/conversations

// Voice
// https://www.twilio.com/docs/api/voice-sdk
//
// iOS: 
//		https://www.twilio.com/docs/api/voice-sdk/ios/getting-started#download-the-application-components
//		https://github.com/twilio/voice-quickstart-objc/archive/master.zip
//		https://github.com/twilio/voice-quickstart-swift/archive/master.zip
//		https://github.com/twilio/voice-callkit-quickstart-swift/archive/master.zip
//		https://github.com/twilio/voice-callkit-quickstart-objc/archive/master.zip
// Android:s
//		https://www.twilio.com/docs/api/voice-sdk/android/getting-started#download-the-application-components

//Voice

var TWILIO_COMMON_ANDROID_VERSION = "0.6";
var TWILIO_COMMON_ANDROID = string.Format ("https://media.twiliocdn.com/sdk/android/common/v{0}/twilio-common-android.aar", TWILIO_COMMON_ANDROID_VERSION);

var TWILIO_IPMESSAGING_ANDROID_VERSION = "0.9.1";
//var TWILIO_IPMESSAGING_ANDROID = string.Format ("https://media.twiliocdn.com/sdk/android/ip-messaging/v0.4/twilio-ip-messaging-android.tar.bz2", TWILIO_IPMESSAGING_ANDROID_VERSION);
//var TWILIO_IPMESSAGING_ANDROID =  "https://www.dropbox.com/s/3hid6sxmbmv0vz6/twilio-ip-messaging-android.jar?dl=1";
//var TWILIO_IPMESSAGING_ANDROID = string.Format ("https://bintray.com/artifact/download/twilio/releases/com/twilio/ip-messaging-android/{0}/ip-messaging-android-{0}.aar", TWILIO_IPMESSAGING_ANDROID_VERSION);
var TWILIO_IPMESSAGING_ANDROID = 
	string.Format
		(
			//"https://media.twiliocdn.com/sdk/android/ip-messaging/v{0}/twilio-ip-messaging-android.tar.bz2"
			//https://bintray.com/twilio/releases/download_file?file_path=com%2Ftwilio%2Fip-messaging-android%2F0.9.1%2Fip-messaging-android-0.9.1.aar
			"https://bintray.com/twilio/releases/download_file?file_path=com%2Ftwilio%2Fip-messaging-android%2F{0}%2Fip-messaging-android-{0}.aar"
			, TWILIO_IPMESSAGING_ANDROID_VERSION
		);

var TWILIO_VIDEO_ANDROID_VERSION = "0.12.2";
var TWILIO_VIDEO_ANDROID = string.Format ("https://bintray.com/artifact/download/twilio/releases/com/twilio/conversations-android/{0}/conversations-android-{0}.aar", TWILIO_VIDEO_ANDROID_VERSION);

var TWILIO_PODSPEC = new [] {
	"source 'https://github.com/twilio/cocoapod-specs'",
	"platform :ios, '8.1'",
	"install! 'cocoapods', :integrate_targets => false",
	"target 'MyApp' do",
	"   pod 'TwilioIPMessagingClient'",
    "   pod 'TwilioConversationsClient'",
	"end",
};

var TARGET = Argument ("target", Argument ("t", "lib"));

Task ("libs")
	//.IsDependentOn ("externals")
	.Does (() =>
{
	NuGetRestore ("./Twilio.sln");
	DotNetBuild ("./Twilio.sln", c => c.Configuration = "Release");
});

Task ("samples").IsDependentOn ("libs").Does (() =>
{
	var sampleSlns = new [] {
		"./samples/TwilioIPMessagingSampleAndroid.sln",
		"./samples/TwilioIPMessagingSampleiOS.sln",
		"./samples/TwilioConversationsSampleAndroid.sln",
		"./samples/TwilioConversationsSampleiOS.sln",
	};

	foreach (var sln in sampleSlns) {
		NuGetRestore (sln);
		DotNetBuild (sln);
	}
});

Task ("externals-android")
	.WithCriteria (!FileExists ("./externals/android/twilio-common-android.jar"))
	.Does (() =>
{
	if (!DirectoryExists ("./externals/android"))
		CreateDirectory ("./externals/android");

	DownloadFile (TWILIO_COMMON_ANDROID, "./externals/android/twilio-common-android.aar");

	DownloadFile (TWILIO_VIDEO_ANDROID, "./externals/android/twilio-conversations-android.aar");

	DownloadFile (TWILIO_IPMESSAGING_ANDROID, "./externals/android/twilio-ip-messaging-android.aar");
	//StartProcess ("tar", "-xvzf ./externals/android/twilio-ip-messaging-android.tar.bz2 -C ./externals/android/");
});
Task ("externals-ios")
	.WithCriteria (!FileExists ("./externals/ios/libTwilioCommon.a"))
	.Does (() =>
{
	if (!DirectoryExists ("./externals/ios"))
		CreateDirectory ("./externals/ios");

	FileWriteLines ("./externals/ios/Podfile", TWILIO_PODSPEC);
	CocoaPodInstall ("./externals/ios", new CocoaPodInstallSettings { NoIntegrate = true });

	CopyFile ("./externals/ios/Pods/TwilioCommon/TwilioCommon.framework/Versions/A/TwilioCommon",
		"./externals/ios/libTwilioCommon.a");
	CopyFile ("./externals/ios/Pods/TwilioIPMessagingClient/TwilioIPMessagingClient.framework/Versions/A/TwilioIPMessagingClient",
		"./externals/ios/libTwilioIPMessagingClient.a");
	CopyFile ("./externals/ios/Pods/TwilioConversationsClient/TwilioConversationsClient.framework/Versions/A/TwilioConversationsClient",
		"./externals/ios/libTwilioConversationsClient.a");
});
Task ("externals").IsDependentOn ("externals-android").IsDependentOn ("externals-ios");

Task ("nuget")
	//.IsDependentOn ("libs")
	.Does (() =>
{
	// NuGet messes up path on mac, so let's add ./ in front twice
	var basePath = IsRunningOnUnix () ? "././" : "./";

	var nuspecs = new FilePath [] {
		"./nuget/Twilio.Common.Xamarin.nuspec",
		"./nuget/Twilio.IPMessaging.Xamarin.nuspec",
		"./nuget/Twilio.Conversations.Xamarin.nuspec",
	};

	foreach (var n in nuspecs) {
		NuGetPack (n, new NuGetPackSettings {
			Verbosity = NuGetVerbosity.Detailed,
			OutputDirectory = "./nuget",
			BasePath = basePath
		});
	}
});

Task ("clean").Does (() =>
{
	if (DirectoryExists ("./externals"))
		DeleteDirectory ("./externals", true);

	DeleteFiles ("./nuget/*.nupkg");

	CleanDirectories ("./**/bin");
	CleanDirectories ("./**/obj");

	if (DirectoryExists ("./tools"))
		DeleteDirectory ("./tools", true);
});

RunTarget (TARGET);
