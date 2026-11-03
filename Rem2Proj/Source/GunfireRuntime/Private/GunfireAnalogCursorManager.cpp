#include "GunfireAnalogCursorManager.h"

UGunfireAnalogCursorManager::UGunfireAnalogCursorManager(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
}

void UGunfireAnalogCursorManager::OnControllerConnectionChanged(bool bConnected) {
}

bool UGunfireAnalogCursorManager::GetAnalogCursorActiveState() {
    return false;
}


