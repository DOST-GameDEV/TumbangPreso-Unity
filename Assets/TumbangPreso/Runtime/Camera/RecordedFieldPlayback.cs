using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // The one way recorded abilities are drawn: every field and flight stroke alive at a recorded moment. Halftime, the
    // spectator's replay and the tag replay all come through here, so a kit looks the same in each (owner, October 31).
    public sealed class RecordedFieldPlayback:IDisposable
    {
        private readonly Transform _parent;
        private readonly Func<int,GameObject> _body,_prop;
        private readonly Dictionary<int,RecordedFieldView> _fields=new Dictionary<int,RecordedFieldView>();
        private readonly Dictionary<int,RecordedFlightStroke> _trails=new Dictionary<int,RecordedFlightStroke>();
        private readonly HashSet<int> _visibleTrails=new HashSet<int>();
        private readonly HashSet<int> _visibleFields=new HashSet<int>();
        public RecordedFieldPlayback(Transform parent,Func<int,GameObject> body,Func<int,GameObject> prop){_parent=parent;_body=body;_prop=prop;}
        /// <summary>Poses everything alive at <paramref name="time"/> and returns the recorded frame it came from.</summary>
        public RecordedFieldFrame Draw(IReadOnlyList<RecordedFieldFrame> frames,float time)
        {
            RecordedFieldFrame frame=null,nextFrame=null;
            for(int i=0;i<frames.Count;i++){var snapshot=frames[i];if(snapshot.Time>time){nextFrame=snapshot;break;}frame=snapshot;}
            _visibleTrails.Clear();
            if(frame!=null)foreach(var trail in frame.Trails)
            {
                _visibleTrails.Add(trail.Id);if(!_trails.TryGetValue(trail.Id,out var view))_trails[trail.Id]=view=new RecordedFlightStroke(_parent,trail);
                RecordedTrail? later=null;if(nextFrame!=null)foreach(var candidate in nextFrame.Trails)if(candidate.Id==trail.Id){later=candidate;break;}
                view.Sample(trail,later,nextFrame!=null?Mathf.InverseLerp(frame.Time,nextFrame.Time,time):0);
            }
            foreach(int id in _trails.Keys.ToArray())if(!_visibleTrails.Contains(id)){_trails[id].Dispose();_trails.Remove(id);}
            _visibleFields.Clear();
            if(frame!=null)foreach(var field in frame.Fields)
            {
                if(field.State.Remaining<=time-frame.Time)continue;
                _visibleFields.Add(field.Id);
                if(_fields.TryGetValue(field.Id,out var existing)&&!existing.Matches(field.State)){existing.Dispose();_fields.Remove(field.Id);}
                if(!_fields.TryGetValue(field.Id,out var view))
                    _fields[field.Id]=view=new RecordedFieldView(_parent,field.State,_body(field.State.Owner),_prop(field.State.Owner));
                var state=field.State;
                if(nextFrame!=null&&(state.Type==RecordedSpecialFields.Kuro||state.Type==RecordedSpecialFields.Ward))
                {
                    foreach(var later in nextFrame.Fields)if(later.Id==field.Id)
                    {
                        float t=Mathf.InverseLerp(frame.Time,nextFrame.Time,time);
                        state.Position=Vector3.Lerp(state.Position,later.State.Position,t);
                        state.Forward=state.Type==RecordedSpecialFields.Kuro?Vector3.Lerp(state.Forward,later.State.Forward,t):Vector3.Slerp(state.Forward,later.State.Forward,t).normalized;
                        break;
                    }
                }
                view.Sample(state,time-frame.Time);
            }
            foreach(int id in _fields.Keys.ToArray())if(!_visibleFields.Contains(id)){_fields[id].Dispose();_fields.Remove(id);}
            return frame;
        }
        public int FieldCount=>_fields.Count;
        public void Visible(bool on)
        {
            foreach(var field in _fields.Values)field.Visible(on);
            foreach(var trail in _trails.Values)trail.Visible(on);
        }
        public void Clear()
        {
            foreach(var field in _fields.Values)field.Dispose();_fields.Clear();
            foreach(var trail in _trails.Values)trail.Dispose();_trails.Clear();
        }
        public void Dispose()=>Clear();
    }
}
