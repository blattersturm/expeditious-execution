/// <reference path="../../../data/@citizenfx/client/index.d.ts" />

const INPUT_REPLAY_START_STOP_RECORDING_SECONDARY = 289;
const freecam = exports.freecam;

let noclipTarget = null;

on('freecam:onFreecamUpdate', () => {
    const position = freecam.GetPosition();
    const rotation = freecam.GetRotation();

    if (noclipTarget) {
        SetEntityCoordsNoOffset(noclipTarget, position[0], position[1], position[2], false, false, false);
        SetEntityRotation(noclipTarget, rotation[0], rotation[1], rotation[2], 0, true);
    }
});

function disableNoclip() {
    SetEntityInvincible(noclipTarget, false);
    FreezeEntityPosition(noclipTarget, false);
    SetEntityCollision(noclipTarget, true, true);
    SetEntityVisible(noclipTarget, true, false);

    if (noclipTarget !== PlayerPedId()) {
        SetEntityVisible(PlayerPedId(), true, false);
    }

    noclipTarget = null;

    freecam.SetEnabled(false);
}

function enableNoclip() {
    noclipTarget = PlayerPedId();

    if (IsPedInAnyVehicle(noclipTarget)) {
        noclipTarget = GetVehiclePedIsIn(noclipTarget, false);
    }

    if (!NetworkHasControlOfEntity(noclipTarget)) {
        noclipTarget = null;
        return;
    }

    SetEntityInvincible(noclipTarget, true);
    FreezeEntityPosition(noclipTarget, true);
    SetEntityCollision(noclipTarget, false, false);
    SetEntityVisible(noclipTarget, false, false);

    if (noclipTarget !== PlayerPedId()) {
        SetEntityVisible(PlayerPedId(), false, false);
    }

    const entityCoords = GetEntityCoords(noclipTarget);

    freecam.SetEnabled(true);
    freecam.SetPosition(...entityCoords);
}

function toggleNoclip() {
    if (freecam.IsEnabled()) {
        disableNoclip();
    } else {
        enableNoclip();
    }
}

setTick(() => {
    if (IsDisabledControlJustPressed(0, INPUT_REPLAY_START_STOP_RECORDING_SECONDARY) && GetLastInputMethod(0) === 1) {
        toggleNoclip();
    }
});