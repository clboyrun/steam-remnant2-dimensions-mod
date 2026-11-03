#include "RemnantUIActor.h"

ARemnantUIActor::ARemnantUIActor(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->TravelLock = EDialogTravelLock::None;
    this->bAllowedDuringTravel = false;
    this->bEnableControllerCursor = false;
}

void ARemnantUIActor::SetCursorStateOnInstance(ECursorOverrideState CursorActiveState) {
}

void ARemnantUIActor::DeviceChangedCallback(int32 ControllerId, EInputDevice CurrentInputDevice, EInputDevice NewInputDevice) {
}


