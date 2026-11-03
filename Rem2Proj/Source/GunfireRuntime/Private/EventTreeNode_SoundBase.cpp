#include "EventTreeNode_SoundBase.h"

UEventTreeNode_SoundBase::UEventTreeNode_SoundBase() {
    this->AutoComplete = false;
    this->CompleteAfterDialog = false;
    this->WaitTimeAfterComplete = 0.10f;
}

void UEventTreeNode_SoundBase::OnFinishedWaiting(TWeakObjectPtr<UEventTreeComponent> Component) const {
}


