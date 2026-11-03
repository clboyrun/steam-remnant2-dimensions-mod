#pragma once
#include "CoreMinimal.h"
#include "EPlayerTravelState.generated.h"

UENUM(BlueprintType)
enum class EPlayerTravelState : uint8 {
    Idle,
    SynchronizingData,
    PendingTravel,
    Traveling,
    PostTravel,
    WaitingForServer,
    WaitingForFinishTravel,
    PendingCharSwap,
    WaitingForClientToBeginTravel,
    WaitingForClientToLoad,
    FinalizingTravel,
    WaitingForClient,
    FindingSpawnLocation,
    LoadingRootQuest,
    LoadingDestinationZone,
    MAX,
};

