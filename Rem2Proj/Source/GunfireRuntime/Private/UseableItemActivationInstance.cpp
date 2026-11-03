#include "UseableItemActivationInstance.h"

UUseableItemActivationInstance::UUseableItemActivationInstance() {
    this->OwningItem = NULL;
}

void UUseableItemActivationInstance::OnOwnerStateChange(const FName& StateName, const FName& PreviousStateName) {
}

void UUseableItemActivationInstance::OnOwnerInteractive(const FName& StateName, AActor* Other) {
}


