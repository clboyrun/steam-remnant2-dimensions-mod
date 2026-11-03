#include "LootContainer.h"
#include "Components/SceneComponent.h"

ALootContainer::ALootContainer(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<USceneComponent>(TEXT("Scene"));
    this->IsGenerating = false;
}

void ALootContainer::OnSpawnListLoaded() {
}

void ALootContainer::Generate(USpawnTable* SpawnTable, const TArray<FName>& SpawnTags) {
}

void ALootContainer::ClearItems() {
}


