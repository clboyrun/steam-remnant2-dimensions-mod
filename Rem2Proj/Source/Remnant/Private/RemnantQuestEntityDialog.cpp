#include "RemnantQuestEntityDialog.h"

URemnantQuestEntityDialog::URemnantQuestEntityDialog(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->Dialog = NULL;
    this->ShouldBeContextProviderForDefaultTree = false;
}

void URemnantQuestEntityDialog::OnActorSpawned(AActor* Actor) {
}


