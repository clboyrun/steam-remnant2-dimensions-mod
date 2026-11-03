#include "CinematicInstance_Base.h"

UCinematicInstance_Base::UCinematicInstance_Base() {
    this->CinematicAsset = NULL;
    this->CurrentSequencePlayer = NULL;
    this->CurrentSequenceActor = NULL;
    this->CurrentSequenceInstigator = NULL;
    this->CurrentSequenceComponent = NULL;
    this->WorldContextObject = NULL;
}

bool UCinematicInstance_Base::WasForceCompleted() {
    return false;
}

void UCinematicInstance_Base::OnFinished() {
}


