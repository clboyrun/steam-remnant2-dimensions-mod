#include "EventTreeNode.h"

UEventTreeNode::UEventTreeNode() {
}

bool UEventTreeNode::Validate_Implementation(UEventTreeComponent* Component) const {
    return false;
}

void UEventTreeNode::OnRestoreState_Implementation(UEventTreeComponent* Component) const {
}

void UEventTreeNode::OnNodeStompEventCallback(const UEventTreeNode* InStompingNode, UEventTreeComponent* Component) {
}

void UEventTreeNode::OnEndCallback(UEventTreeComponent* Component) {
}

void UEventTreeNode::Loop_Implementation(UEventTreeComponent* Component) const {
}

void UEventTreeNode::Join_Implementation(UEventTreeComponent* Component, APlayerController* JoiningController) const {
}

bool UEventTreeNode::IsComplete_Implementation(UEventTreeComponent* Component) const {
    return false;
}

void UEventTreeNode::End_Implementation(UEventTreeComponent* Component) const {
}

bool UEventTreeNode::CanRestore_Implementation(UEventTreeComponent* Component) const {
    return false;
}

void UEventTreeNode::Begin_Implementation(UEventTreeComponent* Component) const {
}


