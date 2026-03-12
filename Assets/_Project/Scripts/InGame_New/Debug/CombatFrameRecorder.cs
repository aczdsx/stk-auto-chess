using System.Collections.Generic;

namespace CookApps.AutoChess
{
    public class CombatFrameRecorder
    {
        private readonly List<CombatFrameSnapshot> _snapshots = new();
        private int _startFrame;
        private int _endFrame;
        private bool _isRecording;

        // 재사용 가능한 스냅샷 풀 (GC 방지)
        private UnitSnapshot[] _unitBuffer = new UnitSnapshot[32];
        private ProjectileSnapshot[] _projectileBuffer = new ProjectileSnapshot[32];
        private SimEventSnapshot[] _eventBuffer = new SimEventSnapshot[128];

        public IReadOnlyList<CombatFrameSnapshot> Snapshots => _snapshots;
        public int SnapshotCount => _snapshots.Count;
        public bool IsRecording => _isRecording;

        public void StartRecording(int startFrame, int endFrame)
        {
            _startFrame = startFrame;
            _endFrame = endFrame;
            _isRecording = true;
            _snapshots.Clear();
        }

        public void StopRecording()
        {
            _isRecording = false;
        }

        public void RecordFrame(CombatMatchState matchState, int frameIndex)
        {
            if (!_isRecording) return;
            if (_startFrame > 0 && frameIndex < _startFrame) return;
            if (_endFrame > 0 && frameIndex > _endFrame)
            {
                _isRecording = false;
                return;
            }
            if (matchState == null) return;

            var snapshot = new CombatFrameSnapshot
            {
                FrameIndex = frameIndex,
            };

            // 유닛 스냅샷
            int unitCount = matchState.UnitCount;
            var units = new UnitSnapshot[unitCount];
            for (int i = 0; i < unitCount; i++)
            {
                ref var u = ref matchState.Units[i];
                units[i] = new UnitSnapshot
                {
                    CombatId = u.CombatId,
                    GridCol = u.GridCol,
                    GridRow = u.GridRow,
                    CurrentHp = u.CurrentHP,
                    MaxHp = u.MaxHP,
                    CurrentMana = u.CurrentMana,
                    MaxMana = u.MaxMana,
                    State = u.State,
                    CurrentTargetId = u.CurrentTargetId,
                    TeamIndex = u.TeamIndex,
                    SkillSpecId = u.SkillSpecId,
                    ChampionSpecId = u.ChampionSpecId,
                    IsAlive = u.IsAlive,
                    Attack = u.Attack,
                    ShieldAmount = u.ShieldAmount,
                    ActiveCC = u.ActiveCC,
                    CCRemainingFrames = u.CCRemainingFrames,
                };
            }
            snapshot.Units = units;
            snapshot.UnitCount = unitCount;

            // 투사체 스냅샷
            int projCount = matchState.ProjectileCount;
            var projectiles = new ProjectileSnapshot[projCount];
            int activeCount = 0;
            for (int i = 0; i < projCount; i++)
            {
                ref var p = ref matchState.Projectiles[i];
                if (!p.IsActive) continue;
                projectiles[activeCount++] = new ProjectileSnapshot
                {
                    ProjectileId = p.ProjectileId,
                    Col = p.CurrentCol,
                    Row = p.CurrentRow,
                    DirCol = p.DirCol,
                    DirRow = p.DirRow,
                    MoveInterval = p.MoveInterval,
                    MoveTimer = p.MoveTimer,
                    HitBehavior = p.HitBehavior,
                    SourceCombatId = p.SourceCombatId,
                    Type = p.Type,
                    Damage = p.Damage,
                    TraveledDistance = p.TraveledDistance,
                    MaxDistance = p.MaxDistance,
                };
            }
            snapshot.Projectiles = projectiles;
            snapshot.ProjectileCount = activeCount;

            // 이벤트 스냅샷 (해당 프레임의 이벤트 큐)
            var eventQueue = matchState.EventQueue;
            int eventCount = eventQueue?.Count ?? 0;
            var events = new SimEventSnapshot[eventCount];
            for (int i = 0; i < eventCount; i++)
            {
                ref var e = ref eventQueue.Events[i];
                events[i] = new SimEventSnapshot
                {
                    Type = e.Type,
                    SourceId = e.EntityId,
                    TargetId = e.TargetEntityId,
                    Value0 = e.Value0,
                    Value1 = e.Value1,
                    Col = e.Col,
                    Row = e.Row,
                    Description = FormatEventDescription(in e),
                };
            }
            snapshot.Events = events;
            snapshot.EventCount = eventCount;

            _snapshots.Add(snapshot);
        }

        public CombatFrameSnapshot GetSnapshot(int index)
        {
            if (index < 0 || index >= _snapshots.Count) return null;
            return _snapshots[index];
        }

        private static string FormatEventDescription(in SimEvent e)
        {
            return e.Type switch
            {
                SimEventType.UnitMoved => $"UnitMoved: #{e.EntityId} -> ({e.Col},{e.Row})",
                SimEventType.UnitAttacked => $"UnitAttacked: #{e.EntityId} -> #{e.TargetEntityId} dmg={e.Value0} crit={e.Flag0}",
                SimEventType.UnitDamaged => $"UnitDamaged: #{e.EntityId} -{e.Value0} HP",
                SimEventType.UnitDied => $"UnitDied: #{e.EntityId}",
                SimEventType.UnitCastSkill => $"UnitCastSkill: #{e.EntityId} skill={e.Value0}",
                SimEventType.UnitHealed => $"UnitHealed: #{e.EntityId} +{e.Value0} HP",
                SimEventType.UnitMissed => $"UnitMissed: #{e.EntityId} -> #{e.TargetEntityId}",
                SimEventType.ProjectileSpawned => $"ProjectileSpawned: P#{e.Value0} src=#{e.EntityId} ({e.Col},{e.Row})",
                SimEventType.ProjectileMoved => $"ProjectileMoved: P#{e.Value0} -> ({e.Col},{e.Row})",
                SimEventType.ProjectileHit => $"ProjectileHit: P#{e.Value0} -> #{e.TargetEntityId}",
                SimEventType.ProjectileExploded => $"ProjectileExploded: ({e.Col},{e.Row}) r={e.Radius}",
                SimEventType.ProjectileExpired => $"ProjectileExpired: P#{e.Value0}",
                SimEventType.SkillAreaEffect => $"SkillAreaEffect: src=#{e.EntityId} ({e.Col},{e.Row}) r={e.Radius}",
                SimEventType.SkillPhaseVfx => $"SkillPhaseVfx: #{e.EntityId} vfx={e.Value0}",
                SimEventType.StatusEffectAdded => $"StatusEffectAdded: #{e.EntityId} type={e.Value0}",
                SimEventType.StatusEffectRemoved => $"StatusEffectRemoved: #{e.EntityId} type={e.Value0}",
                SimEventType.CCAdded => $"CCAdded: #{e.EntityId} type={e.Value0}",
                SimEventType.CCRemoved => $"CCRemoved: #{e.EntityId}",
                SimEventType.ManaFull => $"ManaFull: #{e.EntityId}",
                _ => $"{e.Type}: entity={e.EntityId} target={e.TargetEntityId} v0={e.Value0} v1={e.Value1}",
            };
        }
    }
}
