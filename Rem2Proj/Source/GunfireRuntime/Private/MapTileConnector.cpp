#include "MapTileConnector.h"

UMapTileConnector::UMapTileConnector(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bCanEverAffectNavigation = false;
    this->bHiddenInGame = true;
    this->bUseEditorCompositing = true;
    this->Enabled = true;
}

bool UMapTileConnector::IsConnectorEnabled() {
    return false;
}


