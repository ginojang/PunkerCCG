// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using DG.Tweening;

namespace CCGKit
{
    public class BaseScreen : MonoBehaviour
    {
        public GameObject currentPopup { get; protected set; }
    
        [SerializeField]
        protected Canvas canvas;
    
        [SerializeField]
        protected CanvasGroup panelCanvasGroup;
    
        protected virtual void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    
        protected virtual void Start()
        {
        }
    
        protected virtual void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    
        protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DOTween.KillAll();
        }
    
        public void OpenPopup<T>(string name, Action<T> onOpened = null, bool darkenBackground = true) where T : Popup
        {
            var prefab = Resources.Load<GameObject>(name);
            currentPopup = Instantiate(prefab) as GameObject;
            currentPopup.transform.SetParent(canvas.transform, false);
            currentPopup.GetComponent<Popup>().parentScene = this;
            if (darkenBackground)
            {
                panelCanvasGroup.blocksRaycasts = true;
                panelCanvasGroup.GetComponent<Image>().DOKill();
                panelCanvasGroup.GetComponent<Image>().DOFade(0.5f, 0.5f);
            }
    
            if (onOpened != null)
            {
                onOpened(currentPopup.GetComponent<T>());
            }
        }
    
        public void ClosePopup()
        {
            if (currentPopup != null)
            {
                Destroy(currentPopup);
                currentPopup = null;
                panelCanvasGroup.blocksRaycasts = false;
                panelCanvasGroup.GetComponent<Image>().DOKill();
                panelCanvasGroup.GetComponent<Image>().DOFade(0.0f, 0.2f);
            }
        }
    
        public void OnPopupClosed(Popup popup)
        {
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.GetComponent<Image>().DOKill();
            panelCanvasGroup.GetComponent<Image>().DOFade(0.0f, 0.25f);
        }
    }
}