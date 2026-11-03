#include "MinimapTileSupport.h"
#include "Components/SceneComponent.h"
#include "PersistenceComponent.h"

AMinimapTileSupport::AMinimapTileSupport(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<USceneComponent>(TEXT("SceneComponent"));
    this->PersistenceComponent = CreateDefaultSubobject<UPersistenceComponent>(TEXT("Persistence"));
    this->RevealMinimap = false;
}


