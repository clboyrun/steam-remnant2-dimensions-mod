#include "PlayStateEffect_ScreenFade.h"

UPlayStateEffect_ScreenFade::UPlayStateEffect_ScreenFade() {
    this->FadeDuration = 1.00f;
    this->bHideScreen = false;
    this->bFadeAudio = true;
}

bool UPlayStateEffect_ScreenFade::IsHidingScreen() const {
    return false;
}


