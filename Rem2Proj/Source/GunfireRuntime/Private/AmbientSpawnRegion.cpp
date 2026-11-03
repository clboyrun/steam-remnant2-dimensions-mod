#include "AmbientSpawnRegion.h"
#include "Components/SceneComponent.h"

AAmbientSpawnRegion::AAmbientSpawnRegion(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<USceneComponent>(TEXT("RootComponent"));
    this->SpawnLayer = TEXT("Enemies");
    this->Range = 1000.00f;
    this->AmbientSpawnManager = NULL;
}


