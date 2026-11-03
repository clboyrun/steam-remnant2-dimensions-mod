#include "AIData.h"
#include "Components/SceneComponent.h"

AAIData::AAIData(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<USceneComponent>(TEXT("RootComponent"));
    this->DrawTacticalPoints = true;
    this->DrawTraversalLinks = false;
    this->DrawSpawnPoints = false;
    this->Data = NULL;
}


