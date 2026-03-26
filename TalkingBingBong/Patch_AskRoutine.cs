using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace MikuBongFix
{
    [HarmonyPatch(typeof(Action_AskBingBong), "AskRoutine", new Type[] { typeof(int), typeof(bool) })]
    public static class Patch_AskRoutine
    {
        private const string RuntimeSourceObjectName = "MikuBong_CustomAudio";
        private static readonly string[] ExpectedFiles = new[]
        {
            "response_0.wav",
            "response_1.wav",
            "response_2.wav",
            "response_3.wav"
        };
        private static readonly List<AudioClip> CustomClips = new List<AudioClip>();
        private static readonly List<string> ClipNames = new List<string>();
        private static bool _isLoading;
        private static bool _hasLoadAttempt;
        private static int _nextClipIndex;

        [HarmonyPrefix]
        public static bool Prefix(Action_AskBingBong __instance, int index, bool spamming, ref IEnumerator __result)
        {
            if (__instance == null)
            {
                return true;
            }

            __result = PlayCustomRoutine(__instance, index);
            return false;
        }

        private static IEnumerator PlayCustomRoutine(Action_AskBingBong instance, int index)
        {
            if (instance == null)
            {
                yield break;
            }

            yield return EnsureClipsLoaded();

            if (CustomClips.Count == 0)
            {
                Plugin.Log.LogWarning("[AudioPatch] No custom clips found. Expected files like response_0.wav in plugin directory.");
                yield break;
            }

            int clipIndex = _nextClipIndex;
            _nextClipIndex = (_nextClipIndex + 1) % CustomClips.Count;
            AudioClip clip = CustomClips[clipIndex];
            string clipName = ClipNames[clipIndex];
            if (clip == null)
            {
                Plugin.Log.LogWarning("[AudioPatch] Missing custom clip index " + clipIndex + ".");
                yield break;
            }

            AudioSource playbackSource = GetOrCreatePlaybackSource(instance);
            if (playbackSource != null)
            {
                playbackSource.Stop();
                playbackSource.clip = clip;
                playbackSource.loop = false;
                playbackSource.pitch = 1f;
                playbackSource.Play();
                Plugin.VerboseLog("[AudioPatch] Played custom BingBong clip: " + clipName);
            }
            else
            {
                Plugin.Log.LogWarning("[AudioPatch] No safe playback AudioSource available.");
            }

            if (instance.subtitles != null)
            {
                instance.subtitles.text = "Miku";
                yield return new WaitForSeconds(Mathf.Clamp(clip.length, 0.5f, 4f));
            }
        }

        private static AudioSource GetOrCreatePlaybackSource(Action_AskBingBong instance)
        {
            if (instance == null)
            {
                return null;
            }

            Transform sourceTransform = instance.transform.Find(RuntimeSourceObjectName);
            AudioSource playbackSource = sourceTransform != null ? sourceTransform.GetComponent<AudioSource>() : null;
            if (playbackSource == null)
            {
                GameObject audioObject = new GameObject(RuntimeSourceObjectName);
                audioObject.transform.SetParent(instance.transform, false);
                playbackSource = audioObject.AddComponent<AudioSource>();
            }

            ConfigurePlaybackSource(playbackSource, instance.source);
            return playbackSource;
        }

        private static void ConfigurePlaybackSource(AudioSource playbackSource, AudioSource template)
        {
            if (playbackSource == null)
            {
                return;
            }

            playbackSource.playOnAwake = false;
            playbackSource.loop = false;
            playbackSource.pitch = 1f;
            playbackSource.spatialBlend = template != null ? template.spatialBlend : 1f;
            playbackSource.rolloffMode = template != null ? template.rolloffMode : AudioRolloffMode.Logarithmic;
            playbackSource.minDistance = template != null ? template.minDistance : 1f;
            playbackSource.maxDistance = template != null ? template.maxDistance : 20f;
            playbackSource.spread = template != null ? template.spread : 0f;
            playbackSource.volume = template != null ? template.volume : 1f;
            playbackSource.dopplerLevel = template != null ? template.dopplerLevel : 0f;
            playbackSource.priority = template != null ? template.priority : 128;
        }

        private static IEnumerator EnsureClipsLoaded()
        {
            if (_hasLoadAttempt && !_isLoading)
            {
                yield break;
            }

            if (_isLoading)
            {
                while (_isLoading)
                {
                    yield return null;
                }
                yield break;
            }

            _isLoading = true;
            _hasLoadAttempt = true;
            CustomClips.Clear();
            ClipNames.Clear();

            for (int i = 0; i < ExpectedFiles.Length; i++)
            {
                string fileName = ExpectedFiles[i];
                string audioPath = Path.Combine(Plugin.directory, fileName);
                if (!File.Exists(audioPath))
                {
                    Plugin.Log.LogWarning("[AudioPatch] Missing expected clip file: " + audioPath);
                    continue;
                }

                yield return LoadClip(audioPath, clip =>
                {
                    if (clip != null)
                    {
                        CustomClips.Add(clip);
                        ClipNames.Add(fileName);
                    }
                });
            }

            Plugin.VerboseLog("[AudioPatch] Loaded custom clip count: " + CustomClips.Count);
            _isLoading = false;
        }

        private static IEnumerator LoadClip(string filename, Action<AudioClip> onLoaded)
        {
            Uri fileUri = new Uri(filename);
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(fileUri.AbsoluteUri, AudioType.WAV))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    onLoaded(clip);
                    Plugin.VerboseLog("[AudioPatch] Loaded custom clip: " + filename);
                }
                else
                {
                    onLoaded(null);
                    Plugin.Log.LogError("[AudioPatch] Failed to load audio clip: " + filename + " - " + request.error);
                }
            }
        }
    }
}
