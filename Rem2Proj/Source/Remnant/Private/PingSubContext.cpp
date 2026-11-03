#include "PingSubContext.h"

UPingSubContext::UPingSubContext() {
    this->PingType = 255;
    this->TownLock = ETownLock::NoLock;
    this->Icon = NULL;
    this->WorldIconOverride = NULL;
    this->ShouldOverrideWorldColor = false;
}

bool UPingSubContext::IsValidForPingType(EGamePingType Type) const {
    return false;
}


