#include "AudioUserSetting.h"

UAudioUserSetting::UAudioUserSetting() {
    this->SoundClass = NULL;
    this->bAllowBoostedVolume = false;
}

void UAudioUserSetting::SetSoundClassMultiplier(float InMultiplier) {
}

float UAudioUserSetting::GetSoundClassMultiplier() const {
    return 0.0f;
}


