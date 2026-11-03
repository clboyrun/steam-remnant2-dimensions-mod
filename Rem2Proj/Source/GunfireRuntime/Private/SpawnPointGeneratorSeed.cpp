#include "SpawnPointGeneratorSeed.h"
#include "Components/SceneComponent.h"

ASpawnPointGeneratorSeed::ASpawnPointGeneratorSeed(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bIsEditorOnlyActor = true;
    this->RootComponent = CreateDefaultSubobject<USceneComponent>(TEXT("RootComponent"));
}


