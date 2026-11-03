#include "ResourceSpawnManager.h"

UResourceSpawnManager::UResourceSpawnManager(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->ResourceDef = NULL;
    this->Level = -1;
    this->Generated = false;
}

void UResourceSpawnManager::OnUninitialize() {
}

void UResourceSpawnManager::OnSpawnListLoaded(int32 TileID) {
}

void UResourceSpawnManager::OnSetTileActive(int32 TileID, bool Active) {
}

void UResourceSpawnManager::OnGenerateResources() {
}


