// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 BeBright <98597725+be1bright@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 BeBright <98597725+bebr3ght@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 ImHoks <142083149+ImHoks@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 KillanGenifer <killangenifer@gmail.com>
// SPDX-FileCopyrightText: 2025 SX-7 <sn1.test.preria.2002@gmail.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Inventory;
using Content.Server.Radio.Components;
using Content.Shared._CorvaxNext.Silicons.Borgs.Components;
using Content.Shared.Inventory;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Silicons.Borgs;

/// <summary>
/// Server-side logic for borg type switching. Handles more heavyweight and server-specific switching logic.
/// </summary>
public sealed partial class BorgSwitchableTypeSystem : SharedBorgSwitchableTypeSystem // DeltaV: Made partial
{
    [Dependency] private readonly BorgSystem _borgSystem = default!;
    [Dependency] private readonly ServerInventorySystem _inventorySystem = default!;

    protected override void SelectBorgModule(Entity<BorgSwitchableTypeComponent> ent, ProtoId<BorgTypePrototype> borgType, ProtoId<BorgSubtypePrototype> borgSubtype)
    {
        var prototype = Prototypes.Index(borgType);

        // Assign radio channels
        string[] radioChannels = [.. ent.Comp.InherentRadioChannels, .. prototype.RadioChannels];
        if (TryComp(ent, out IntrinsicRadioTransmitterComponent? transmitter))
            transmitter.Channels = [.. radioChannels];

        if (TryComp(ent, out ActiveRadioComponent? activeRadio))
            activeRadio.Channels = [.. radioChannels];

        // Corvax-Next-AiRemoteControl-Start
        if (TryComp(ent, out AiRemoteControllerComponent? aiRemoteComp))
        {
            if (TryComp(aiRemoteComp.AiHolder, out IntrinsicRadioTransmitterComponent? stationAiTransmitter) && transmitter != null)
            {
                aiRemoteComp.PreviouslyTransmitterChannels = [.. radioChannels];
                transmitter.Channels = [.. stationAiTransmitter.Channels];
            }

            if (TryComp(aiRemoteComp.AiHolder, out ActiveRadioComponent? stationAiActiveRadio) && activeRadio != null)
            {
                aiRemoteComp.PreviouslyActiveRadioChannels = [.. radioChannels];
                activeRadio.Channels = [.. stationAiActiveRadio.Channels];
            }
        }
        // Corvax-Next-AiRemoteControl-End

        // Borg transponder for the robotics console
        if (TryComp(ent, out BorgTransponderComponent? transponder))
        {
            _borgSystem.SetTransponderSprite(
                (ent.Owner, transponder),
                new SpriteSpecifier.Rsi(new ResPath("Mobs/Silicon/chassis.rsi"), prototype.SpriteBodyState));

            _borgSystem.SetTransponderName(
                (ent.Owner, transponder),
                Loc.GetString($"borg-type-{borgType}-transponder"));
        }

        // Configure modules
        if (TryComp(ent, out BorgChassisComponent? chassis))
        {
            var chassisEnt = (ent.Owner, chassis);
            _borgSystem.SetMaxModules(
                chassisEnt,
                prototype.ExtraModuleCount + prototype.DefaultModules.Length);

            _borgSystem.SetModuleWhitelist(chassisEnt, prototype.ModuleWhitelist);

            foreach (var module in prototype.DefaultModules)
            {
                var moduleEntity = Spawn(module);
                var borgModule = Comp<BorgModuleComponent>(moduleEntity);
                _borgSystem.SetBorgModuleDefault((moduleEntity, borgModule), true);
                _borgSystem.InsertModule(chassisEnt, moduleEntity);
            }
        }

        // Begin DeltaV Code: Custom lawset patching
        if (prototype.Lawset is {} lawset)
            ConfigureLawset(ent, lawset);
        // End DeltaV Code

        // Configure special components
        if (Prototypes.TryIndex(ent.Comp.SelectedBorgType, out var previousPrototype))
        {
            if (previousPrototype.AddComponents is { } removeComponents)
                EntityManager.RemoveComponents(ent, removeComponents);
        }

        if (prototype.AddComponents is { } addComponents)
        {
            EntityManager.AddComponents(ent, addComponents);
        }

        // Configure inventory template (used for hat spacing)
        if (TryComp(ent, out InventoryComponent? inventory))
        {
            _inventorySystem.SetTemplateId((ent.Owner, inventory), prototype.InventoryTemplateId);
        }

        base.SelectBorgModule(ent, borgType, borgSubtype);
    }
}
