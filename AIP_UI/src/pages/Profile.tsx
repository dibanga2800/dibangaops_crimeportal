import { useState, useEffect, ChangeEvent } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Switch } from '@/components/ui/switch'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Separator } from '@/components/ui/separator'
import { toast } from '@/components/ui/use-toast'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog'
import { Save, Eye, EyeOff, Loader2, KeyRound, ShieldCheck, ShieldOff } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { ProfilePictureCapture } from '@/components/profile/ProfilePictureCapture'
import { userService } from '@/services/userService'
import { api } from '@/config/api'

interface SecuritySettings {
  twoFactorAuth: boolean
  emailNotifications: boolean
  loginAlerts: boolean
}

const Profile = () => {
  const { user, updateProfilePicture } = useAuth()

  const [profile, setProfile] = useState({
    firstName: user?.firstName ?? '',
    lastName: user?.lastName ?? '',
    email: user?.email ?? '',
    phone: user?.phoneNumber ?? '',
    jobTitle: user?.jobTitle ?? '',
    department: '',
    location: '',
    bio: '',
  })

  const [securitySettings, setSecuritySettings] = useState<SecuritySettings>(() => ({
    twoFactorAuth: !!user?.twoFactorEnabled,
    emailNotifications: user?.emailNotificationsEnabled ?? true,
    loginAlerts: user?.loginAlertsEnabled ?? true,
  }))

  // Sync security settings from user when it loads/changes (from DB)
  useEffect(() => {
    if (user) {
      setSecuritySettings({
        twoFactorAuth: !!user.twoFactorEnabled,
        emailNotifications: user.emailNotificationsEnabled ?? true,
        loginAlerts: user.loginAlertsEnabled ?? true,
      })
    }
  }, [user?.id, user?.twoFactorEnabled, user?.emailNotificationsEnabled, user?.loginAlertsEnabled])

  // Remove legacy localStorage key (security settings now in DB)
  useEffect(() => {
    try {
      localStorage.removeItem('securitySettings')
    } catch { /* ignore */ }
  }, [])

  // Change password state
  const [isPasswordDialogOpen, setIsPasswordDialogOpen] = useState(false)
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  })
  const [showPasswords, setShowPasswords] = useState({
    current: false,
    new: false,
    confirm: false,
  })
  const [passwordErrors, setPasswordErrors] = useState<Record<string, string>>({})
  const [isChangingPassword, setIsChangingPassword] = useState(false)
  const [isSavingProfile, setIsSavingProfile] = useState(false)

  // 2FA confirmation dialog
  const [is2FADialogOpen, setIs2FADialogOpen] = useState(false)
  const [pending2FAState, setPending2FAState] = useState(false)
  const [isUpdatingSecurity, setIsUpdatingSecurity] = useState(false)

  const handleProfileChange = (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target
    setProfile(prev => ({ ...prev, [name]: value }))
  }

  const handleSecurityToggle = async (setting: keyof SecuritySettings, value?: boolean) => {
    const newValue = value ?? !securitySettings[setting]

    if (setting === 'twoFactorAuth') {
      setPending2FAState(newValue)
      setIs2FADialogOpen(true)
      return
    }

    const payload: Record<string, boolean> = {}
    if (setting === 'emailNotifications') payload.emailNotificationsEnabled = newValue
    if (setting === 'loginAlerts') payload.loginAlertsEnabled = newValue

    setIsUpdatingSecurity(true)
    try {
      const updatedUser = await userService.updateMyProfile(payload)
      setSecuritySettings(prev => ({ ...prev, [setting]: newValue }))
      window.dispatchEvent(new CustomEvent('user-assignments-updated', { detail: updatedUser }))
      const label = setting === 'emailNotifications' ? 'Email notifications' : 'Login alerts'
      toast({
        title: 'Setting updated',
        description: `${label} ${newValue ? 'enabled' : 'disabled'}.`,
      })
    } catch (error) {
      console.error('❌ [Profile] Failed to update security setting:', error)
      toast({
        title: 'Failed to update setting',
        description: 'Could not save your preference. Please try again.',
        variant: 'destructive',
      })
    } finally {
      setIsUpdatingSecurity(false)
    }
  }

  const confirm2FAToggle = async () => {
    setIsUpdatingSecurity(true)
    try {
      const updatedUser = await userService.updateMyProfile({
        twoFactorEnabled: pending2FAState,
      })
      setSecuritySettings(prev => ({ ...prev, twoFactorAuth: pending2FAState }))
      window.dispatchEvent(new CustomEvent('user-assignments-updated', { detail: updatedUser }))
      setIs2FADialogOpen(false)
      toast({
        title: pending2FAState ? 'Two-factor authentication enabled' : 'Two-factor authentication disabled',
        description: pending2FAState
          ? 'Your account now requires a second verification step when logging in.'
          : 'Two-factor authentication has been turned off.',
      })
    } catch (error) {
      console.error('❌ [Profile] Failed to update 2FA setting:', error)
      toast({
        title: 'Failed to update setting',
        description: 'Could not save your preference. Please try again.',
        variant: 'destructive',
      })
    } finally {
      setIsUpdatingSecurity(false)
    }
  }

  // Password validation
  const validatePassword = (): boolean => {
    const errors: Record<string, string> = {}

    if (!passwordForm.currentPassword) {
      errors.currentPassword = 'Current password is required'
    }

    if (!passwordForm.newPassword) {
      errors.newPassword = 'New password is required'
    } else if (passwordForm.newPassword.length < 8) {
      errors.newPassword = 'Password must be at least 8 characters'
    } else if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(passwordForm.newPassword)) {
      errors.newPassword = 'Password must include uppercase, lowercase, and a number'
    }

    if (!passwordForm.confirmPassword) {
      errors.confirmPassword = 'Please confirm your new password'
    } else if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      errors.confirmPassword = 'Passwords do not match'
    }

    if (passwordForm.currentPassword && passwordForm.newPassword &&
        passwordForm.currentPassword === passwordForm.newPassword) {
      errors.newPassword = 'New password must be different from current password'
    }

    setPasswordErrors(errors)
    return Object.keys(errors).length === 0
  }

  const handleChangePassword = async () => {
    if (!validatePassword()) return

    setIsChangingPassword(true)
    try {
      const response = await api.post('/Auth/change-password', {
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword,
        confirmNewPassword: passwordForm.confirmPassword,
      })

      const apiResponse = response.data as {
        Success?: boolean
        success?: boolean
        Message?: string
        message?: string
        Errors?: string[]
        errors?: string[]
      }

      const success = apiResponse.Success ?? apiResponse.success ?? false
      const message = apiResponse.Message ?? apiResponse.message ?? ''
      const errors = apiResponse.Errors ?? apiResponse.errors

      if (!success) {
        const errorMessage =
          (Array.isArray(errors) && errors.length > 0 && errors.join(', ')) ||
          message ||
          'Failed to change password'

        toast({
          title: 'Failed to change password',
          description: errorMessage,
          variant: 'destructive',
        })
        return
      }

      toast({
        title: 'Password changed',
        description: 'Your password has been updated successfully.',
      })
      setIsPasswordDialogOpen(false)
      resetPasswordForm()
    } catch (error) {
      console.error('❌ [Profile] Failed to change password:', error)
      toast({
        title: 'Failed to change password',
        description: 'Could not update your password. Please verify your current password and try again.',
        variant: 'destructive',
      })
    } finally {
      setIsChangingPassword(false)
    }
  }

  const resetPasswordForm = () => {
    setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' })
    setPasswordErrors({})
    setShowPasswords({ current: false, new: false, confirm: false })
  }

  const handlePasswordDialogClose = (open: boolean) => {
    if (!open) resetPasswordForm()
    setIsPasswordDialogOpen(open)
  }

  const handleSaveProfilePicture = async (imageDataUrl: string) => {
    if (!user?.id) {
      toast({
        title: 'Unable to update profile picture',
        description: 'User information is not available. Please log in again.',
        variant: 'destructive',
      })
      return
    }

    // Update UI and local cache immediately
    updateProfilePicture(imageDataUrl)

    try {
      const updatedUser = await userService.updateMyProfile({
        profilePicture: imageDataUrl,
      })

      // Sync AuthContext/sessionStore with backend state
      window.dispatchEvent(new CustomEvent('user-assignments-updated', { detail: updatedUser }))

      toast({
        title: 'Profile picture updated',
        description: 'Your profile picture has been saved and is now visible across the application.',
      })
    } catch (error) {
      console.error('❌ [Profile] Failed to update profile picture:', error)
      toast({
        title: 'Failed to update profile picture',
        description: 'An error occurred while saving your profile picture. Please try again.',
        variant: 'destructive',
      })
    }
  }

  const handleRemoveProfilePicture = async () => {
    if (!user?.id) {
      toast({
        title: 'Unable to update profile picture',
        description: 'User information is not available. Please log in again.',
        variant: 'destructive',
      })
      return
    }

    // Update UI and local cache immediately
    updateProfilePicture(null)

    try {
      const updatedUser = await userService.updateMyProfile({
        clearProfilePicture: true,
      })

      window.dispatchEvent(new CustomEvent('user-assignments-updated', { detail: updatedUser }))

      toast({
        title: 'Profile picture removed',
        description: 'Your profile picture has been removed.',
      })
    } catch (error) {
      console.error('❌ [Profile] Failed to remove profile picture:', error)
      toast({
        title: 'Failed to remove profile picture',
        description: 'An error occurred while removing your profile picture. Please try again.',
        variant: 'destructive',
      })
    }
  }

  const handleSaveProfile = async () => {
    if (!user?.id) {
      toast({
        title: 'Unable to update profile',
        description: 'User information is not available. Please log in again.',
        variant: 'destructive',
      })
      return
    }

    setIsSavingProfile(true)
    try {
      const updatedUser = await userService.updateMyProfile({
        firstName: profile.firstName.trim(),
        lastName: profile.lastName.trim(),
        email: profile.email.trim(),
        phoneNumber: profile.phone.trim() || undefined,
        jobTitle: profile.jobTitle.trim() || undefined,
      })

      // Notify AuthContext (and any other listeners) so the in-memory user is kept in sync
      window.dispatchEvent(new CustomEvent('user-assignments-updated', { detail: updatedUser }))

      toast({
        title: 'Profile updated',
        description: 'Your profile information has been successfully saved.',
      })
    } catch (error) {
      console.error('❌ [Profile] Failed to update profile:', error)
      toast({
        title: 'Failed to update profile',
        description: 'An error occurred while saving your changes. Please try again.',
        variant: 'destructive',
      })
    } finally {
      setIsSavingProfile(false)
    }
  }

  const userInitials = user
    ? `${user.firstName?.charAt(0) ?? ''}${user.lastName?.charAt(0) ?? ''}`.toUpperCase()
    : 'U'

  const userName = user ? `${user.firstName} ${user.lastName}` : 'User'

  return (
    <div className="h-full">
      <div className="flex items-center justify-between border-b px-6 py-4">
        <div>
          <h1 className="text-2xl font-semibold">Profile Settings</h1>
          <p className="text-sm text-muted-foreground">
            Manage your account settings and preferences
          </p>
        </div>
      </div>

      <div className="p-4 md:p-6 space-y-6">
        <Tabs defaultValue="personal" className="w-full">
          <TabsList className="mb-4">
            <TabsTrigger value="personal">Personal Information</TabsTrigger>
            <TabsTrigger value="security">Security Settings</TabsTrigger>
          </TabsList>

          <TabsContent value="personal" className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Profile Picture</CardTitle>
                <CardDescription>
                  Take a photo with your camera or upload an image
                </CardDescription>
              </CardHeader>
              <CardContent className="flex flex-col sm:flex-row items-center gap-6">
                <ProfilePictureCapture
                  currentImage={user?.profilePicture}
                  onSave={handleSaveProfilePicture}
                  onRemove={handleRemoveProfilePicture}
                  userName={userName}
                  userInitials={userInitials}
                />
                <div className="space-y-2 text-center sm:text-left">
                  <h3 className="font-medium">Your Profile Photo</h3>
                  <p className="text-sm text-muted-foreground">
                    Click the avatar or camera icon to take a new photo
                    or upload an image.
                    <br />
                    JPG, PNG, GIF or WebP. Max size 2MB.
                  </p>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Personal Information</CardTitle>
                <CardDescription>Update your personal details</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="firstName">First Name</Label>
                    <Input
                      id="firstName"
                      name="firstName"
                      value={profile.firstName}
                      onChange={handleProfileChange}
                      placeholder="Enter your first name"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="lastName">Last Name</Label>
                    <Input
                      id="lastName"
                      name="lastName"
                      value={profile.lastName}
                      onChange={handleProfileChange}
                      placeholder="Enter your last name"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="email">Email</Label>
                    <Input
                      id="email"
                      name="email"
                      value={profile.email}
                      onChange={handleProfileChange}
                      type="email"
                      placeholder="Enter your email"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="phone">Phone Number</Label>
                    <Input
                      id="phone"
                      name="phone"
                      value={profile.phone}
                      onChange={handleProfileChange}
                      type="tel"
                      placeholder="Enter your phone number"
                    />
                  </div>
                </div>

                <Separator className="my-4" />

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="jobTitle">Job Title</Label>
                    <Input
                      id="jobTitle"
                      name="jobTitle"
                      value={profile.jobTitle}
                      onChange={handleProfileChange}
                      placeholder="Enter your job title"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="department">Department</Label>
                    <Input
                      id="department"
                      name="department"
                      value={profile.department}
                      onChange={handleProfileChange}
                      placeholder="Enter your department"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="location">Location</Label>
                    <Input
                      id="location"
                      name="location"
                      value={profile.location}
                      onChange={handleProfileChange}
                      placeholder="Enter your location"
                    />
                  </div>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="bio">Bio</Label>
                  <textarea
                    id="bio"
                    name="bio"
                    value={profile.bio}
                    onChange={handleProfileChange}
                    rows={3}
                    className="w-full min-h-[80px] rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                    placeholder="Tell us a bit about yourself"
                  />
                </div>

                <Button onClick={handleSaveProfile} className="mt-2" disabled={isSavingProfile}>
                  {isSavingProfile ? (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  ) : (
                    <Save className="mr-2 h-4 w-4" />
                  )}
                  {isSavingProfile ? 'Saving...' : 'Save Changes'}
                </Button>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="security" className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Security Settings</CardTitle>
                <CardDescription>Manage your security preferences</CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                {/* Two-Factor Authentication */}
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label className="flex items-center gap-2">
                      {securitySettings.twoFactorAuth
                        ? <ShieldCheck className="h-4 w-4 text-green-600" />
                        : <ShieldOff className="h-4 w-4 text-muted-foreground" />
                      }
                      Two-factor Authentication
                    </Label>
                    <p className="text-sm text-muted-foreground">
                      {securitySettings.twoFactorAuth
                        ? 'Active — your account requires a second verification step'
                        : 'Add an extra layer of security to your account'
                      }
                    </p>
                  </div>
                  <Switch
                    checked={securitySettings.twoFactorAuth}
                    onCheckedChange={(checked) => handleSecurityToggle('twoFactorAuth', checked)}
                    disabled={isUpdatingSecurity}
                  />
                </div>

                <Separator />

                {/* Email Notifications */}
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Email Notifications</Label>
                    <p className="text-sm text-muted-foreground">
                      Receive security alerts via email
                    </p>
                  </div>
                  <Switch
                    checked={securitySettings.emailNotifications}
                    onCheckedChange={(checked) => handleSecurityToggle('emailNotifications', checked)}
                    disabled={isUpdatingSecurity}
                  />
                </div>

                <Separator />

                {/* Login Alerts */}
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Login Alerts</Label>
                    <p className="text-sm text-muted-foreground">
                      Get notified when someone logs into your account
                    </p>
                  </div>
                  <Switch
                    checked={securitySettings.loginAlerts}
                    onCheckedChange={(checked) => handleSecurityToggle('loginAlerts', checked)}
                    disabled={isUpdatingSecurity}
                  />
                </div>

                <Separator />

                {/* Change Password */}
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label className="flex items-center gap-2">
                      <KeyRound className="h-4 w-4 text-muted-foreground" />
                      Password
                    </Label>
                    <p className="text-sm text-muted-foreground">
                      Update your account password
                    </p>
                  </div>
                  <Button variant="outline" onClick={() => setIsPasswordDialogOpen(true)}>
                    Change Password
                  </Button>
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>

      {/* Change Password Dialog */}
      <Dialog open={isPasswordDialogOpen} onOpenChange={handlePasswordDialogClose}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Change Password</DialogTitle>
            <DialogDescription>
              Enter your current password and choose a new one.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-2">
            {/* Current Password */}
            <div className="space-y-2">
              <Label htmlFor="currentPassword">Current Password</Label>
              <div className="relative">
                <Input
                  id="currentPassword"
                  type={showPasswords.current ? 'text' : 'password'}
                  value={passwordForm.currentPassword}
                  onChange={(e) => {
                    setPasswordForm(prev => ({ ...prev, currentPassword: e.target.value }))
                    setPasswordErrors(prev => ({ ...prev, currentPassword: '' }))
                  }}
                  placeholder="Enter current password"
                  className={passwordErrors.currentPassword ? 'border-destructive' : ''}
                />
                <button
                  type="button"
                  onClick={() => setShowPasswords(prev => ({ ...prev, current: !prev.current }))}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                  aria-label={showPasswords.current ? 'Hide password' : 'Show password'}
                >
                  {showPasswords.current ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {passwordErrors.currentPassword && (
                <p className="text-xs text-destructive">{passwordErrors.currentPassword}</p>
              )}
            </div>

            {/* New Password */}
            <div className="space-y-2">
              <Label htmlFor="newPassword">New Password</Label>
              <div className="relative">
                <Input
                  id="newPassword"
                  type={showPasswords.new ? 'text' : 'password'}
                  value={passwordForm.newPassword}
                  onChange={(e) => {
                    setPasswordForm(prev => ({ ...prev, newPassword: e.target.value }))
                    setPasswordErrors(prev => ({ ...prev, newPassword: '' }))
                  }}
                  placeholder="Enter new password"
                  className={passwordErrors.newPassword ? 'border-destructive' : ''}
                />
                <button
                  type="button"
                  onClick={() => setShowPasswords(prev => ({ ...prev, new: !prev.new }))}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                  aria-label={showPasswords.new ? 'Hide password' : 'Show password'}
                >
                  {showPasswords.new ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {passwordErrors.newPassword ? (
                <p className="text-xs text-destructive">{passwordErrors.newPassword}</p>
              ) : (
                <p className="text-xs text-muted-foreground">
                  Min 8 characters with uppercase, lowercase, and a number
                </p>
              )}
            </div>

            {/* Confirm Password */}
            <div className="space-y-2">
              <Label htmlFor="confirmPassword">Confirm New Password</Label>
              <div className="relative">
                <Input
                  id="confirmPassword"
                  type={showPasswords.confirm ? 'text' : 'password'}
                  value={passwordForm.confirmPassword}
                  onChange={(e) => {
                    setPasswordForm(prev => ({ ...prev, confirmPassword: e.target.value }))
                    setPasswordErrors(prev => ({ ...prev, confirmPassword: '' }))
                  }}
                  placeholder="Confirm new password"
                  className={passwordErrors.confirmPassword ? 'border-destructive' : ''}
                  onKeyDown={(e) => { if (e.key === 'Enter') handleChangePassword() }}
                />
                <button
                  type="button"
                  onClick={() => setShowPasswords(prev => ({ ...prev, confirm: !prev.confirm }))}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                  aria-label={showPasswords.confirm ? 'Hide password' : 'Show password'}
                >
                  {showPasswords.confirm ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {passwordErrors.confirmPassword && (
                <p className="text-xs text-destructive">{passwordErrors.confirmPassword}</p>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => handlePasswordDialogClose(false)}>
              Cancel
            </Button>
            <Button onClick={handleChangePassword} disabled={isChangingPassword}>
              {isChangingPassword ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <KeyRound className="mr-2 h-4 w-4" />
              )}
              Update Password
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* 2FA Confirmation Dialog */}
      <Dialog open={is2FADialogOpen} onOpenChange={setIs2FADialogOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>
              {pending2FAState ? 'Enable Two-Factor Authentication?' : 'Disable Two-Factor Authentication?'}
            </DialogTitle>
            <DialogDescription>
              {pending2FAState
                ? 'You will be required to enter a verification code each time you log in. This adds an extra layer of security to your account.'
                : 'Removing two-factor authentication will make your account less secure. Are you sure you want to continue?'
              }
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="gap-2 sm:gap-0">
            <Button variant="outline" onClick={() => setIs2FADialogOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={confirm2FAToggle}
              variant={pending2FAState ? 'default' : 'destructive'}
            >
              {pending2FAState ? 'Enable 2FA' : 'Disable 2FA'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

export default Profile
