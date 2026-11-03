#include "MapTileTemplate.h"

AMapTileTemplate::AMapTileTemplate(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->TileFlags = 255;
    this->Direction = ETileDirection::None;
    this->TileRating = -1;
    this->Optional = false;
}


