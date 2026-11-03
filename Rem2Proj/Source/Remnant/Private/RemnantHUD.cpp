#include "RemnantHUD.h"

ARemnantHUD::ARemnantHUD(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->DebugDisplay.AddDefaulted(1);
    this->ContextActor = NULL;
}








bool ARemnantHUD::ShouldShowSubtitles() const {
    return false;
}

bool ARemnantHUD::ShouldShowHealthBars() const {
    return false;
}







bool ARemnantHUD::HasInvalidDialog() const {
    return false;
}










