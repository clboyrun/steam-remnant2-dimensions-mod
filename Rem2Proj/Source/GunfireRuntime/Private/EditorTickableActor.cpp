#include "EditorTickableActor.h"

AEditorTickableActor::AEditorTickableActor(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->IsTickableInGame = false;
    this->TickEditorInterval = 1.00f;
    this->TickEditorDelta = 1.00f;
}



