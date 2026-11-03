#include "MapGenSettings.h"

AMapGenSettings::AMapGenSettings(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->TileSet = NULL;
    this->bOverrideAudioVolume = false;
}


