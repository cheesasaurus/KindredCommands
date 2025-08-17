using ProjectM.Network;
using ProjectM;
using Unity.Entities;
using ProjectM.Shared;
using Stunlock.Core;

namespace KindredCommands;
internal class Buffs
{
	public static bool TryAddBuff(Entity User, Entity Character, PrefabGUID buffPrefab, int duration = 0, bool immortal = true)
	{
		if (BuffUtility.HasBuff(Core.Server.EntityManager, Character, buffPrefab.ToIdentifier()))
		{
			return false; // todo: is there any actual use case for not updating an existing buff?
		}

		if (!TryGetOrCreateBuff(User, Character, buffPrefab, out var buffEntity))
		{
			return false;
		}
		UpdateBuff(buffEntity, duration, immortal);
		return true;
	}

	public static bool TryGetOrCreateBuff(Entity User, Entity Character, PrefabGUID buffPrefab, out Entity buffEntity)
	{
		if (BuffUtility.TryGetBuff(Core.Server.EntityManager, Character, buffPrefab, out buffEntity))
		{
			return true;
		}

		var des = Core.Server.GetExistingSystemManaged<DebugEventsSystem>();
		var buffEvent = new ApplyBuffDebugEvent()
		{
			BuffPrefabGUID = buffPrefab
		};
		var fromCharacter = new FromCharacter()
		{
			User = User,
			Character = Character
		};
		des.ApplyBuff(fromCharacter, buffEvent);
		
		if (!BuffUtility.TryGetBuff(Core.Server.EntityManager, Character, buffPrefab, out buffEntity))
		{
			return false;
		}

		if (buffEntity.Has<CreateGameplayEventsOnSpawn>())
		{
			buffEntity.Remove<CreateGameplayEventsOnSpawn>();
		}
		if (buffEntity.Has<GameplayEventListeners>())
		{
			buffEntity.Remove<GameplayEventListeners>();
		}
		return true;
	}	

	public static void UpdateBuff(Entity buffEntity, int duration = 0, bool immortal = true)
	{
		if (immortal)
		{
			buffEntity.Add<Buff_Persists_Through_Death>();
			if (buffEntity.Has<RemoveBuffOnGameplayEvent>())
			{
				buffEntity.Remove<RemoveBuffOnGameplayEvent>();
			}

			if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>())
			{
				buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
			}
		}

		if (duration > -1 && duration != 0)
		{
			if (!buffEntity.Has<LifeTime>())
			{
				buffEntity.Add<LifeTime>();
				buffEntity.Write(new LifeTime
				{
					EndAction = LifeTimeEndAction.Destroy
				});
			}

			var lifetime = buffEntity.Read<LifeTime>();
			lifetime.Duration = duration;
			buffEntity.Write(lifetime);
		}
		else if (duration == -1)
		{
			if (buffEntity.Has<LifeTime>())
			{
				var lifetime = buffEntity.Read<LifeTime>();
				lifetime.EndAction = LifeTimeEndAction.None;
				buffEntity.Write(lifetime);
			}
			if (buffEntity.Has<RemoveBuffOnGameplayEvent>())
			{
				buffEntity.Remove<RemoveBuffOnGameplayEvent>();
			}
			if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>())
			{
				buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
			}
		}
	}

	public static void RemoveBuff(Entity Character, PrefabGUID buffPrefab)
	{
		if (BuffUtility.TryGetBuff(Core.EntityManager, Character, buffPrefab, out var buffEntity))
		{
			DestroyUtility.Destroy(Core.EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
		}
	}
}
