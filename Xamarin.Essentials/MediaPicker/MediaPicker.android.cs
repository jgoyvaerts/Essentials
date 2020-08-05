﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Provider;

namespace Xamarin.Essentials
{
    public static partial class MediaPicker
    {
        const int requestCodeMediaPicker = 12348;
        const int requestCodeMediaCapture = 12349;

        static bool PlatformIsPhotoCaptureAvailable
            => Platform.AppContext.PackageManager.HasSystemFeature(PackageManager.FeatureCameraAny);

        static Task<MediaPickerResult> PlatformPickPhotoAsync(MediaPickerOptions options)
            => PlatformPickAsync(options, true);

        static Task<MediaPickerResult> PlatformPickVideoAsync(MediaPickerOptions options)
            => PlatformPickAsync(options, false);

        static async Task<MediaPickerResult> PlatformPickAsync(MediaPickerOptions options, bool photo)
        {
            // we only need the permission when accessing the file, but it's more natural
            // to ask the user first, then show the picker.
            await Permissions.RequestAsync<Permissions.StorageRead>();

            var intent = new Intent(Intent.ActionGetContent);
            intent.SetType(photo ? "image/*" : "video/*");

            var pickerIntent = Intent.CreateChooser(intent, options?.Title);

            try
            {
                var result = await IntermediateActivity.StartAsync(pickerIntent, requestCodeMediaPicker);

                return new MediaPickerResult(result.Data);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        static Task<MediaPickerResult> PlatformCapturePhotoAsync(MediaPickerOptions options)
            => PlatformCaptureAsync(options, true);

        static Task<MediaPickerResult> PlatformCaptureVideoAsync(MediaPickerOptions options)
            => PlatformCaptureAsync(options, false);

        static async Task<MediaPickerResult> PlatformCaptureAsync(MediaPickerOptions options, bool photo)
        {
            await Permissions.EnsureGrantedAsync<Permissions.Camera>();
            await Permissions.EnsureGrantedAsync<Permissions.StorageWrite>();

            var capturePhotoIntent = new Intent(photo ? MediaStore.ActionImageCapture : MediaStore.ActionVideoCapture);
            if (capturePhotoIntent.ResolveActivity(Platform.AppContext.PackageManager) != null)
            {
                try
                {
                    var activity = Platform.GetCurrentActivity(true);

                    var storageDir = Platform.AppContext.ExternalCacheDir;
                    var tmpFile = Java.IO.File.CreateTempFile(Guid.NewGuid().ToString(), photo ? ".jpg" : ".mp4", storageDir);
                    tmpFile.DeleteOnExit();

                    capturePhotoIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
                    capturePhotoIntent.AddFlags(ActivityFlags.GrantWriteUriPermission);

                    var result = await IntermediateActivity.StartAsync(capturePhotoIntent, requestCodeMediaCapture, tmpFile);

                    var outputUri = result.GetParcelableExtra(IntermediateActivity.OutputUriExtra) as global::Android.Net.Uri;

                    return new MediaPickerResult(outputUri);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            }

            return null;
        }
    }

    public partial class MediaPickerResult
    {
        internal MediaPickerResult(global::Android.Net.Uri contentUri)
            : base(contentUri)
        {
        }
    }
}
