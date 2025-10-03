using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.Helpers;
using TootTallyCore.Utils.TootTallyGlobals;
using UnityEngine;

namespace TootTallyAutoToot
{
    public class AutoTootController : MonoBehaviour
    {
        private GameController _gameController;
        private GameObject _pointer;
        private TMP_Text _autoTootText;
        private RectTransform _pointerRect;

        private float _lastTimeSample;
        private float _trackTime, _lastTrackTime, _estimatedTrackTime;
        private float _lastNoteStartTime, _lastNoteEndTime, _currentNoteStartTime, _currentNoteEndTime;
        private float _lastNoteStartY, _lastNoteEndY, _currentNoteStartY, _currentNoteEndY;
        private bool _isSlider;
        private int _noteIndex;
        private float _earlyTimingAdjustValue, _lateTimingAdjustValue;
        private bool _releasedBetweenNotes;
        private bool _shouldBreath;
        private bool _isNoteActive;

        public bool isEnabled;
        public bool isTooting;
        public Vector2 pointerPosition;

        private static Func<float, float> _currentEasing;

        public void Init(GameController gameController)
        {
            _gameController = gameController;
            _pointer = _gameController.pointer;
            _pointerRect = _gameController.pointerrect;
            isEnabled = false;
            isTooting = false;
            _isSlider = false;
            _shouldBreath = false;
            _isNoteActive = false;
            _releasedBetweenNotes = true;
            _noteIndex = -1;
            if (_gameController.leveldata.Count > 0)
            {
                _currentNoteStartTime = B2s(_gameController.leveldata[0][0], _gameController.tempo);
                _currentNoteEndTime = _currentNoteStartTime + B2s(_gameController.leveldata[0][1], _gameController.tempo);
                _currentNoteStartY = _gameController.leveldata[0][2];
                _currentNoteEndY = _gameController.leveldata[0][4];
            }
            _lastNoteStartY = 0;
            _lastNoteEndY = 0;
            _lastNoteEndTime = -999;
            _lastNoteStartTime = -999;
            _lastTrackTime = _trackTime = 0f;
            _lastTimeSample = 0f;
            pointerPosition = _pointerRect.anchoredPosition;
            _currentEasing = EasingHelper.GetCurrentEasing(Plugin.Instance.EasingType.Value);
            _earlyTimingAdjustValue = Plugin.Instance.EarlyTimingAdjust.Value / 1000f * TootTallyGlobalVariables.gameSpeedMultiplier;
            _lateTimingAdjustValue = Plugin.Instance.LateTimingAdjust.Value / 1000f * TootTallyGlobalVariables.gameSpeedMultiplier;
            _autoTootText = GameObjectFactory.CreateSingleText(_gameController.ui_score_shadow.transform.parent.parent, "AutoTootText", "AutoToot Enabled", GameObjectFactory.TextFont.Multicolore);
            _autoTootText.rectTransform.anchoredPosition = new Vector2(0, Plugin.Instance.DistanceFromBottom.Value);
            _autoTootText.rectTransform.anchorMax = _autoTootText.rectTransform.anchorMin = new Vector2(.5f, 0);
            _autoTootText.rectTransform.pivot = new Vector2(.5f, 1f);
            _autoTootText.rectTransform.sizeDelta = new Vector2(200, 14);
            _autoTootText.fontSize = Plugin.Instance.TextSize.Value;
            _autoTootText.fontStyle = FontStyles.Italic | FontStyles.UpperCase;
            _autoTootText.gameObject.SetActive(false);
        }

        public void Update()
        {
            if (_gameController.freeplay) return;

            if (Input.GetKeyDown(Plugin.Instance.ToggleKey.Value))
                ToggleEnable();

            if (!_gameController.paused && !_gameController.quitting && _gameController.musictrack.isPlaying)
                UpdateTrackData();

            if (!isEnabled) return;

            TootTallyGlobalVariables.usedAutotoot = true;

            if (Plugin.Instance.PerfectPlay.Value)
                _gameController.breathcounter = 0f;
            else if (!_shouldBreath && ((_gameController.breathcounter >= .95f && _isNoteActive) || (!_isNoteActive && _gameController.breathcounter >= .5f)))
                _shouldBreath = true;
            else if (_shouldBreath && ((_gameController.breathcounter <= .65f && _isNoteActive) || (!_isNoteActive && _gameController.breathcounter <= 0f)))
                _shouldBreath = false;

            isTooting = ShouldToot();
            if (!isTooting)
                _releasedBetweenNotes = true;
            pointerPosition.y = GetPositionY();
            _pointerRect.anchoredPosition = pointerPosition;
        }

        private void UpdateTrackData()
        {
            var dt = Time.deltaTime;
            _trackTime += dt * TootTallyGlobalVariables.gameSpeedMultiplier;
            if (_lastTimeSample != _gameController.musictrack.timeSamples)
            {
                _lastTrackTime = _gameController.musictrack.time - _gameController.noteoffset - _gameController.latency_offset;
                _lastTimeSample = _gameController.musictrack.timeSamples;
            }
            //slight correction
            _trackTime += (_lastTrackTime - _trackTime) / 60f;

            if (_trackTime >= _currentNoteEndTime)
            {
                _noteIndex++;
                if (_noteIndex + 1 < _gameController.leveldata.Count)
                {
                    _lastNoteStartTime = _currentNoteStartTime;
                    _lastNoteEndTime = _currentNoteEndTime + _lateTimingAdjustValue;
                    _lastNoteStartY = _currentNoteStartY;
                    _lastNoteEndY = _currentNoteEndY;

                    _isSlider = Mathf.Abs(_gameController.leveldata[_noteIndex + 1][0] - (_gameController.leveldata[_noteIndex][0] + _gameController.leveldata[_noteIndex][1])) < 0.05f;
                    _currentNoteStartTime = B2s(_gameController.leveldata[_noteIndex + 1][0], _gameController.tempo);
                    _currentNoteEndTime = _currentNoteStartTime + B2s(_gameController.leveldata[_noteIndex + 1][1], _gameController.tempo);
                    _currentNoteStartY = _gameController.leveldata[_noteIndex + 1][2];
                    _currentNoteEndY = _gameController.leveldata[_noteIndex + 1][4];

                    _lastNoteEndTime = Mathf.Min(_lastNoteEndTime, _currentNoteStartTime - .01f);

                    _releasedBetweenNotes = !isTooting;
                }
                else
                {
                    _currentNoteStartTime = float.MaxValue;
                    _isSlider = false;
                }

            }

            _isNoteActive = _trackTime >= _currentNoteStartTime - dt * 5f && _trackTime < _currentNoteEndTime + dt * 5f;
        }

        public void ToggleEnable()
        {
            isEnabled = !isEnabled;
            _gameController.controllermode = isEnabled;
            _lastNoteEndTime = _trackTime - .01f;
            _lastNoteEndY = _pointerRect.anchoredPosition.y;
            Plugin.LogInfo($"AutoToot {(isEnabled ? "Enabled" : "Disabled")}.");
            _autoTootText.gameObject.SetActive(isEnabled);
        }

        //if you're not tooting, should you start tooting? else should you stop
        private bool ShouldToot() => ((_trackTime >= Mathf.Max(_currentNoteStartTime - (Plugin.Instance.SyncTootWithSong.Value ? _gameController.latency_offset : _earlyTimingAdjustValue), _lastNoteEndTime) && _releasedBetweenNotes)
                                     || _trackTime <= _lastNoteEndTime
                                     || _isSlider)
                                     && !_shouldBreath
                                     && _trackTime > .01f;


        private float GetPositionY()
        {
            float by;
            if (_trackTime >= _currentNoteStartTime - _earlyTimingAdjustValue && _trackTime <= _currentNoteEndTime + _lateTimingAdjustValue)
            {
                if (_currentNoteStartY != _currentNoteEndY)
                    by = Mathf.Clamp(1f - ((_currentNoteEndTime - _trackTime - (.005555f * TootTallyGlobalVariables.gameSpeedMultiplier)) / (_currentNoteEndTime - _currentNoteStartTime)), 0, 1);
                else
                    by = Mathf.Clamp(1f - ((_currentNoteEndTime - _trackTime) / (_currentNoteEndTime - (_currentNoteStartTime - _earlyTimingAdjustValue))), 0, 1);
                return _currentNoteStartY + _gameController.easeInOutVal(Mathf.Abs(by), 0f, _currentNoteEndY - _currentNoteStartY, 1f);
            }
            var adjustedNoteStart = _currentNoteStartTime - _earlyTimingAdjustValue;
            by = Mathf.Clamp(1f - ((adjustedNoteStart - _trackTime) / (adjustedNoteStart - _lastNoteEndTime)), 0, 1);
            return Mathf.Lerp(_lastNoteEndY, _currentNoteStartY, _currentEasing(by));
        }

        public static float B2s(float time, float bpm) => time / bpm * 60f;

    }
}
