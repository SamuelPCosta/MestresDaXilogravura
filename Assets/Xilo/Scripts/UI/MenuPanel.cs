using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Xilo.Core;

namespace Xilo.UI
{
    public sealed class MenuPanel : MonoBehaviour
    {
        const string LogCategory = "Menu";

        [SerializeField] Button startButton;
        [SerializeField] string workshopScene = "Room";

        bool isLoading;

        void Awake()
        {
            if (startButton == null)
            {
                XiloLog.Error(LogCategory, "Start button not assigned", this);
                enabled = false;
                return;
            }

            startButton.onClick.AddListener(StartExperience);
        }

        void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(StartExperience);
        }

        // Loads the workshop scene once, ignoring repeated clicks while loading.
        public void StartExperience()
        {
            if (isLoading)
                return;

            try
            {
                isLoading = true;
                XiloLog.Info(LogCategory, $"Loading scene {workshopScene}", this);
                SceneManager.LoadSceneAsync(workshopScene);
            }
            catch (Exception exception)
            {
                isLoading = false;
                XiloLog.Exception(LogCategory, exception, this);
            }
        }
    }
}
