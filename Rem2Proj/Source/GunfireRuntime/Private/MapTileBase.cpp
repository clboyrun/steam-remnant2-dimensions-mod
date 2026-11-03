#include "MapTileBase.h"
#include "Components/BillboardComponent.h"

AMapTileBase::AMapTileBase(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<UBillboardComponent>(TEXT("Root"));
    this->Overlay = false;
    this->Enabled = true;
    this->TileSet = NULL;
}


