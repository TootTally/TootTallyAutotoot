using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TootTallyCore.Utils.Helpers;
using TootTallyCore.Utils.TootTallyGlobals;
using UnityEngine;

namespace TootTallyAutoToot
{
    public class AutoTootController : MonoBehaviour
    {
        private GameController _gameController;
        private GameObject _pointer;
        private RectTransform _pointerRect;
        private Vector2 _pointerPosition;

        private float _lastTimeSample;
        private float _trackTime, _lastTrackTime, _estimatedTrackTime;
        private float _lastNoteStartTime, _lastNoteEndTime, _currentNoteStartTime, _currentNoteEndTime;
        private float _lastNoteStartY, _lastNoteEndY, _currentNoteStartY, _currentNoteEndY;
        private bool _isSlider;
        private int _noteIndex;
        private float _timingAdjustValue;

        public bool isEnabled;
        public bool isTooting;

        private static Func<float, float> _currentEasing;

        public void Init(GameController gameController)
        {
            _gameController = gameController;
            _pointer = _gameController.pointer;
            _pointerRect = _gameController.pointerrect;
            isEnabled = false;
            isTooting = false;
            _isSlider = false;
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
            _lastNoteEndTime = -1;
            _lastNoteStartTime = -1;
            _lastTrackTime = _trackTime = 0f;
            _lastTimeSample = 0f;
            _pointerPosition = _pointerRect.anchoredPosition;
            _currentEasing = EasingHelper.GetCurrentEasing(Plugin.Instance.EasingType.Value);
            //_timingAdjustValue = B2s(.05f, _gameController.tempo);
            _timingAdjustValue = .02f;
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

            isTooting = ShouldToot();
            _pointerPosition.y = GetPositionY();
            _pointerRect.anchoredPosition = _pointerPosition;
        }

        private void UpdateTrackData()
        {
            _trackTime += Time.deltaTime * TootTallyGlobalVariables.gameSpeedMultiplier;
            _trackTime += (_lastTrackTime - _trackTime) / 90f;
            if (_lastTimeSample != _gameController.musictrack.timeSamples)
            {
                _lastTrackTime = _gameController.musictrack.time - _gameController.noteoffset - _gameController.latency_offset;
                _lastTimeSample = _gameController.musictrack.timeSamples;
            }

            if (_trackTime >= _currentNoteEndTime)
            {
                _noteIndex++;
                if (_noteIndex + 1 < _gameController.leveldata.Count)
                {
                    _lastNoteStartTime = _currentNoteStartTime;
                    _lastNoteEndTime = _currentNoteEndTime;
                    _lastNoteStartY = _currentNoteStartY;
                    _lastNoteEndY = _currentNoteEndY;

                    _isSlider = Mathf.Abs(_gameController.leveldata[_noteIndex + 1][0] - (_gameController.leveldata[_noteIndex][0] + _gameController.leveldata[_noteIndex][1])) < 0.05f;
                    _currentNoteStartTime = B2s(_gameController.leveldata[_noteIndex + 1][0], _gameController.tempo);
                    _currentNoteEndTime = _currentNoteStartTime + B2s(_gameController.leveldata[_noteIndex + 1][1], _gameController.tempo);
                    _currentNoteStartY = _gameController.leveldata[_noteIndex + 1][2];
                    _currentNoteEndY = _gameController.leveldata[_noteIndex + 1][4];
                }
                else
                    _currentNoteStartTime = float.MaxValue;
            }

        }

        public void ToggleEnable()
        {
            isEnabled = !isEnabled;
            _gameController.controllermode = isEnabled;
            Plugin.LogInfo($"AutoToot {(isEnabled ? "Enabled" : "Disabled")}.");
        }

        //if you're not tooting, should you start tooting? else should you stop
        private bool ShouldToot() => _trackTime >= _currentNoteStartTime + (Plugin.Instance.SyncTootWithSong.Value ? _gameController.latency_offset : 0) - _timingAdjustValue
                                     || _trackTime <= _lastNoteEndTime + (Plugin.Instance.SyncTootWithSong.Value ? _gameController.latency_offset : 0) + _timingAdjustValue
                                     || _isSlider;

        private float GetPositionY()
        {
            float by;
            if ((_trackTime >= _currentNoteStartTime && _trackTime <= _currentNoteEndTime) || _isSlider)
            {
                by = Mathf.Clamp(1f - ((_currentNoteEndTime - _trackTime) / (_currentNoteEndTime - _currentNoteStartTime)), 0, 1);
                return _currentNoteStartY + _gameController.easeInOutVal(Mathf.Abs(by), 0f, _gameController.currentnotepshift, 1f);
            }
            var adjustedNoteStart = _currentNoteStartTime - _timingAdjustValue;
            var adjustedNoteEnd = _lastNoteEndTime + _timingAdjustValue;
            by = Mathf.Clamp(1f - ((adjustedNoteStart - _trackTime) / (adjustedNoteStart - adjustedNoteEnd)), 0, 1);
            return Mathf.Lerp(_lastNoteEndY, _currentNoteStartY, _currentEasing(by));
        }


        public static float B2s(float time, float bpm) => time / bpm * 60f;

    }
}
