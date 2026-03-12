import React from 'react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription
} from '@/components/ui/dialog'
import { UserForm } from './UserForm'
import { User, CreateUserInput, UpdateUserInput } from '@/types/user'

interface UserDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user?: User
  onSubmit: (data: CreateUserInput | UpdateUserInput) => void
}

export const UserDialog = ({
  open,
  onOpenChange,
  user,
  onSubmit,
}: UserDialogProps) => {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl">
        <div className="max-h-[80vh] overflow-y-auto pr-2">
          <DialogHeader>
            <DialogTitle>
              {user ? 'Edit User' : 'Create New User'}
            </DialogTitle>
            <DialogDescription>
              Enter the user details below. All required fields must be completed.
            </DialogDescription>
          </DialogHeader>
          <UserForm
            initialData={user}
            onSubmit={onSubmit}
            onCancel={() => onOpenChange(false)}
          />
        </div>
      </DialogContent>
    </Dialog>
  )
}
