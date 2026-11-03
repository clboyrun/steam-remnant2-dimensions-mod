#include "EventTreeNode_Base.h"

UEventTreeNode_Base::UEventTreeNode_Base() {
    this->Enabled = true;
    this->DoOnce = false;
    this->DoOnceExpiration = -1.00f;
    this->Seed = 23100;
    this->UniqueId = 0;
}

FString UEventTreeNode_Base::GetEditorLabel_Implementation() const {
    return TEXT("");
}


