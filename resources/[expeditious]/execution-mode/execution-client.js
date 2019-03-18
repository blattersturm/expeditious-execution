/// <reference path="../../../data/@citizenfx/client/index.d.ts" />

let firstSpawn = true;

function WaitFor(predicate, interval = 0) {
    return new Promise((resolve) => {
        const timer = setInterval(() => {
            if (predicate()) {
                clearInterval(timer);
                resolve();
            }
        }, interval);
    });
}

async function FindNearbyCoords(coords) {
    let success = false;
    let tries = 0;

    do {
        // make sure nav meshes have loaded around it
        AddNavmeshRequiredRegion(coords[0], coords[1], 750.01);

        const attemptStart = GetGameTimer();

        await WaitFor(() => IsNavmeshLoadedInArea(coords[0] - 10.0, coords[1] - 10.0, 0.0, coords[0] + 10.0, coords[1] + 10.0, 100.01) || (GetGameTimer() - attemptStart) > 500);

        // get a safe coord
        const [ successValue, theseCoords ] = GetSafeCoordForPed(coords[0], coords[1], coords[2], false, 16);
        success = !!successValue;

        // remove navmesh requirement
        RemoveNavmeshRequiredRegions();

        if (!success) {
            // try new coords
            coords[0] += GetRandomFloatInRange(-500.01, 500.01);
            coords[1] += GetRandomFloatInRange(-500.01, 500.01);
            coords[2] += 50.0;
        } else {
            coords = theseCoords;
        }

        ++tries;

        if (tries > 10) {
            coords = [ 0.0, 0.0, 0.0 ];
            break;
        }
    } while (!success);

    return coords;
}

let spawnLock = false;

async function trySpawn() {
    if (spawnLock) {
        return;
    }

    spawnLock = true;

    let coords = [0.0, 0.0, 0.0];

    if (firstSpawn) {
        while (coords[0] == 0.0) {
            // get a coordinate to spawn around
            let xCoord = GetRandomFloatInRange(-3000.01, 3000.01);
            let yCoord = GetRandomFloatInRange(-3000.01, 6000.01);
            let zCoord = GetRandomFloatInRange(10.01, 110.01);

            // try finding a good coordinate near it
            coords = await FindNearbyCoords([ xCoord, yCoord, zCoord ]);
        }
    } else {
        while (coords[0] == 0.0) {
            // try spawning somewhere around our current coords
            const firstCoords = GetEntityCoords(PlayerPedId(), false);

            firstCoords[0] += GetRandomFloatInRange(-250.01, 250.01);
            firstCoords[1] += GetRandomFloatInRange(-250.01, 250.01);

            coords = await FindNearbyCoords(firstCoords);
        }
    }

    const spawn = {
        x: coords[0],
        y: coords[1],
        z: coords[2],
        heading: 0.0,
        model: 'a_m_m_skater_01'
    };

    exports.spawnmanager.spawnPlayer(spawn, () => {
        spawnLock = false;
    });

    firstSpawn = false;
}

on('onClientGameTypeStart', () => {
    SetEntityCoords(PlayerPedId(), 1.5, 1.5, 1.5);
    FreezeEntityPosition(PlayerPedId(), true);

    // async callbacks directly used as ref don't work, so trigger it from inside a void ref
    exports.spawnmanager.setAutoSpawnCallback(() => {
        trySpawn();
    });

    exports.spawnmanager.forceRespawn()
});

on('onClientGameTypeStop', () => {
    DoScreenFadeOut(0);
});